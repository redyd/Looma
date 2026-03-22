COMMAND TO PUBLISH:
dotnet publish src/Looma.App/Looma.App.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/linux

dotnet publish src/Looma.App/Looma.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/win


VELOPACK
vpk pack \
  --packId Looma \
  --packVersion 1.0.0 \
  --packDir ./publish/linux \
  --mainExe Looma.App \
  --outputDir ./releases/linux

vpk pack \
  --packId Looma \
  --packVersion 1.0.0 \
  --packDir ./publish/win \
  --mainExe Looma.App.exe \
  --outputDir ./releases/win
