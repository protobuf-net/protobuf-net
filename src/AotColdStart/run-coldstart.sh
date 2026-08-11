#!/usr/bin/env bash
# Cold-start race: A vanilla (runtime model), B generated model on the same JIT build, C the same
# generated model published native-AOT.
#
# The loop is *here*, not in the program, and that is the whole point: what is being measured happens
# once per process — the runtime model inspecting metadata and emitting IL on first use — so an
# in-process loop would measure iterations 2..N, which are warm, and report the opposite of the
# answer. Each launch contributes exactly one sample.
#
# Two clocks per sample. The program reports its own elapsed time from the top of Main, isolating the
# serialization work; this script times the whole process, which includes host startup and is what a
# user actually feels. They differ most for the native build, so quoting only one would flatter it.
#
# usage: run-coldstart.sh [runs]
set -u
cd "$(dirname "$0")/../.."

RUNS="${1:-30}"
JIT="src/AotColdStart/bin/Release/net10.0/AotColdStart"
NATIVE="src/AotColdStart/bin/Release/net10.0/linux-x64/publish/AotColdStart"

median() { sort -n | awk '{ v[NR]=$1 } END { if (NR==0) { print "n/a"; exit } m=int((NR+1)/2); print (NR%2 ? v[m] : (v[m]+v[m+1])/2) }'; }

sample() { # <label> <binary> <mode>
    local label="$1" bin="$2" mode="$3" i start end
    local -a wall internal
    for ((i = 0; i < RUNS; i++)); do
        start=$(date +%s%N)
        out=$("$bin" "$mode" 2>/dev/null)
        end=$(date +%s%N)
        wall+=( "$(( (end - start) / 1000 ))" )               # microseconds
        internal+=( "$(echo "$out" | cut -f2)" )              # milliseconds, from the program
    done
    local w i2
    w=$(printf '%s\n' "${wall[@]}" | median)
    i2=$(printf '%s\n' "${internal[@]}" | median)
    printf '%-34s %10.1f %12s\n' "$label" "$(echo "$w / 1000" | bc -l)" "$i2"
}

if [[ ! -x "$JIT" ]]; then echo "build first: dotnet build src/AotColdStart -c Release" >&2; exit 1; fi

echo "cold start, median of $RUNS process launches (ms)"
echo
printf '%-34s %10s %12s\n' "" "wall" "in-process"
sample "baseline (no serialization)"        "$JIT"    baseline
sample "A  vanilla, runtime model"          "$JIT"    vanilla
sample "B  generated model, same build"     "$JIT"    generated
if [[ -x "$NATIVE" ]]; then
    sample "C  generated model, native AOT"  "$NATIVE" generated
    sample "   native baseline"              "$NATIVE" baseline
else
    echo "   (C skipped: publish with -r linux-x64 to include it)"
fi
