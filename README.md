<h1 align="center">Сервер обновление приложений и скачивания установочных файлов</h1>

docker build -t updateserver .

docker run -p 8888:80  -e login={login} -e password={password} -v {pathtoprograms}:/app/programs  --name updateserver updateserver
