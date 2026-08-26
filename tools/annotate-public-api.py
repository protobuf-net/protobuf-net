"""Rewrite PublicAPI baselines from the analyzer's own RS0036 output.

Enabling <Nullable>enable</Nullable> on a library whose PublicAPI.*.txt files are OBLIVIOUS makes
RS0037 fire ("PublicAPI.txt is missing '#nullable enable'"). Adding that marker then makes RS0036
fire once per entry - and RS0036 names the EXACT annotated signature it wants, so the replacement
line is never guessed: strip the `!`/`?` back off and that is the un-annotated entry to find.

That is what makes gap B51 tractable at all: the three big baselines are ~3,000 lines between them,
and this drives the rewrite from the compiler rather than by hand.

Usage:
    dotnet build <project> -c Debug --no-incremental > build.log 2>&1
    python tools/annotate-public-api.py build.log src/<project>/PublicAPI
    # re-run the build; RS0036 should be silent

Add `#nullable enable` as the first line of each PublicAPI.Shipped.txt first - RS0036 does not fire
until the file opts in. Re-run the build/script pair until it reports 0 unmatched: a signature can
be reported only once the ones before it are settled.
"""
import re, sys, os, glob, collections

log, roots = sys.argv[1], sys.argv[2:]
msg = re.compile(r"warning RS0036: Symbol '(.+?)' is missing nullability annotations")
wanted = []
seen = set()
for m in msg.finditer(open(log, encoding='utf-8', errors='ignore').read()):
    sig = m.group(1)
    if sig in seen: continue
    seen.add(sig); wanted.append(sig)

def bare(sig): return sig.replace('!', '').replace('?', '')

files = []
for root in roots:
    files += glob.glob(os.path.join(root, '**', 'PublicAPI.*.txt'), recursive=True)

index = {}
for f in files:
    raw = open(f, 'rb').read()
    bom = raw.startswith(b'\xef\xbb\xbf')
    lines = raw.decode('utf-8-sig').split('\n')
    index[f] = [bom, lines]

applied, missing = 0, []
for sig in wanted:
    target, hit = bare(sig), False
    for f, (bom, lines) in index.items():
        for i, line in enumerate(lines):
            if line.strip() == target:
                lines[i] = line.replace(target, sig)
                applied += 1; hit = True; break
        if hit: break
    if not hit: missing.append(sig)

for f, (bom, lines) in index.items():
    out = '\n'.join(lines).encode('utf-8')
    open(f, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + out)

print('annotated %d of %d' % (applied, len(wanted)))
for s in missing[:10]: print('  UNMATCHED:', s)
