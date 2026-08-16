"""One-off: where do the bytes of a realistic protobuf payload actually go, and how are the
varint magnitudes biased? Walks the wire format and classifies every byte.

Usage: python census.py <payload.bin>
"""
import sys, collections

data = open(sys.argv[1], "rb").read()

# byte budget, by role
budget = collections.Counter()
# how many varints of each encoded width, by role
widths = collections.Counter()
# magnitudes of the length prefixes, bucketed
lengths = []
# field-number population, by encoded tag width
tagfields = collections.Counter()
ambiguous = [0]
magnitudes = collections.Counter()


def varint(buf, i):
    start = i
    v = 0
    shift = 0
    while True:
        if i >= len(buf):
            raise ValueError("truncated varint")
        b = buf[i]
        v |= (b & 0x7F) << shift
        i += 1
        if not (b & 0x80):
            return v, i, i - start
        shift += 7
        if shift > 63:
            raise ValueError("varint too long")


def looks_like_message(buf):
    """Exact parse to the end, every field plausible. Used only as a tie-break."""
    if not buf:
        return False
    i = 0
    try:
        while i < len(buf):
            tag, i, _ = varint(buf, i)
            field, wire = tag >> 3, tag & 7
            if field == 0 or field > 536870911 or wire in (6, 7):
                return False
            if wire == 0:
                _, i, _ = varint(buf, i)
            elif wire == 1:
                i += 8
            elif wire == 2:
                n, i, _ = varint(buf, i)
                i += n
            elif wire == 5:
                i += 4
            else:  # groups: not expected in a descriptor set
                return False
            if i > len(buf):
                return False
        return i == len(buf)
    except ValueError:
        return False


def printable(buf):
    return bool(buf) and all(0x20 <= b < 0x7F or b in (9, 10, 13) for b in buf)


def walk(buf, depth):
    i = 0
    while i < len(buf):
        tag, i, tw = varint(buf, i)
        field, wire = tag >> 3, tag & 7
        budget["tag"] += tw
        widths[("tag", tw)] += 1
        tagfields[(tw, field)] += 1

        if wire == 0:
            v, i, vw = varint(buf, i)
            budget["varint value"] += vw
            widths[("varint value", vw)] += 1
            # a false bool is not written at all (the write guard), so every bool on the wire
            # is 1 - which makes "varint value == 1" the upper bound on bool fields
            if v <= 1:
                magnitudes[v] += 1
        elif wire == 1:
            budget["fixed64"] += 8
            i += 8
        elif wire == 5:
            budget["fixed32"] += 4
            i += 4
        elif wire == 2:
            n, i, lw = varint(buf, i)
            budget["length prefix"] += lw
            widths[("length prefix", lw)] += 1
            lengths.append(n)
            payload = buf[i:i + n]
            # a name is ASCII and often ALSO parses as a message; prefer the string reading,
            # which is right for a descriptor set and is where the ambiguity lives
            as_msg = looks_like_message(payload)
            if as_msg and printable(payload):
                ambiguous[0] += 1
            if as_msg and not printable(payload):
                walk(payload, depth + 1)
            else:
                budget["string/bytes payload"] += n
            i += n
        else:
            raise ValueError(f"unexpected wire type {wire}")


walk(data, 0)

total = sum(budget.values())
print(f"# Payload byte census ({len(data)} bytes)\n")
assert total == len(data), f"classified {total} of {len(data)}"

print("| role | bytes | share |")
print("| --- | ---: | ---: |")
for role, n in budget.most_common():
    print(f"| {role} | {n} | {100.0 * n / total:.1f}% |")
print(f"| **total** | **{total}** | 100% |\n")

print("## Varints, by encoded width\n")
print("| role | width | count | bytes |")
print("| --- | ---: | ---: | ---: |")
for (role, w), c in sorted(widths.items()):
    print(f"| {role} | {w} | {c} | {c * w} |")

nlen = len(lengths)
print(f"\n## Length prefixes ({nlen} of them)\n")
buckets = [(0, 0), (1, 127), (128, 16383), (16384, 2097151)]
print("| magnitude | count | share |")
print("| --- | ---: | ---: |")
for lo, hi in buckets:
    c = sum(1 for x in lengths if lo <= x <= hi)
    if c:
        print(f"| {lo}..{hi} | {c} | {100.0 * c / nlen:.1f}% |")

tags1 = sum(c for (w, f), c in tagfields.items() if w == 1)
tags2 = sum(c for (w, f), c in tagfields.items() if w == 2)
tagsN = sum(c for (w, f), c in tagfields.items() if w > 2)
tt = tags1 + tags2 + tagsN
print(f"\n## Tags written: {tt}\n")
print("| tag width | count | share |")
print("| --- | ---: | ---: |")
for label, c in [("1 byte (fields 1-15)", tags1), ("2 bytes (fields 16-2047)", tags2), ("3+ bytes", tagsN)]:
    if c:
        print(f"| {label} | {c} | {100.0 * c / tt:.1f}% |")

nv = sum(c for (role, w), c in widths.items() if role == "varint value")
print(f"\n## Varint VALUES that are 0 or 1 (the bool ceiling)\n")
print("| value | count | share of all varint values |")
print("| --- | ---: | ---: |")
for v in sorted(magnitudes):
    print(f"| {v} | {magnitudes[v]} | {100.0 * magnitudes[v] / nv:.1f}% |")

print(f"\n(ambiguous length-delimited payloads - printable AND parseable as a message, "
      f"read as strings: {ambiguous[0]})")
