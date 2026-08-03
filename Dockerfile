# 階段 1：編譯環境
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

# 複製專案檔並還原套件 (利用 Docker 快取優化速度)
COPY *.csproj ./
RUN dotnet restore

# 複製其餘程式碼並發布
COPY . ./
RUN dotnet publish LeaveApi.csproj -c Release -o /app/out

# 階段 2：執行環境 (只複製發布後的檔案，縮小 Image 體積)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

# 設定 Container 內部引導的連接埠
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LeaveApi.dll"]