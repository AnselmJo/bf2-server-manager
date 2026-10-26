#!/bin/sh
# Linux build (Mono mcs + mingw windres) of the Windows exe. CI builds on Windows with Build.cmd.
# Compiles against the official .NET Framework 4.5 reference assemblies (NOT Mono's own libraries,
# which contain extra overloads that do not exist on Windows).
set -e
cd "$(dirname "$0")"
REF=refs/v4.5
if [ ! -f "$REF/mscorlib.dll" ]; then
  echo "Missing $REF: put the .NET Framework 4.5 reference assemblies (mscorlib.dll, System*.dll) into build/refs/v4.5 first (see docs/BUILDING.md)." >&2
  exit 1
fi
printf '#!/bin/sh\nfor a in "$@"; do l="$a"; done\ncat "$l"\n' > /tmp/pp.sh; chmod +x /tmp/pp.sh
x86_64-w64-mingw32-windres --preprocessor=/tmp/pp.sh -O res -i app.rc -o app.res
R="-r:$REF/mscorlib.dll -r:$REF/System.dll -r:$REF/System.Core.dll -r:$REF/System.Drawing.dll -r:$REF/System.Windows.Forms.dll -r:$REF/System.IO.Compression.dll -r:$REF/System.IO.Compression.FileSystem.dll -r:$REF/System.Web.Extensions.dll"
mkdir -p ../out
mcs -langversion:5 -target:winexe -codepage:65001 -optimize+ -nostdlib -noconfig -win32res:app.res \
  -out:../out/BF2Manager.exe -resource:../src/Server/coopbots.py,coopbots.py $R -recurse:'../src/*.cs'
if [ "$1" = "test" ]; then
  mcs -langversion:5 -target:exe -codepage:65001 -nostdlib -noconfig -main:BF2Coop.Tests.Runner \
    -out:../out/Tests.exe -resource:../src/Server/coopbots.py,coopbots.py $R -recurse:'../src/*.cs' -recurse:'../tests/*.cs'
fi
rm -f app.res
