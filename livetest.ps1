$exist = where.exe csharprepl.exe
if(-not $exist){
    Write-Host "Insta"
    Write-Host "C# REPL is not installed. Install it with 'dotnet tool install -g csharprepl'" -ea stop
}
$dllPath = join-path $PSScriptRoot "CueSheetNet\bin\Debug\net8.0\CueSheetNet.dll"

# $nugetPath =  ls (join-path $PSScriptRoot CueSheetNet\bin\) -recurse -filter *.nupkg | sort fullname | select -last 1
if(-not $dllPath){
    Write-Error "No nuget file!" -ea stop
}
Write-host "Adding $dllPath"
pushd $PSScriptRoot
csharprepl  $PSScriptRoot/setup.csx -r $dllPath -u CueSheetNet
popd