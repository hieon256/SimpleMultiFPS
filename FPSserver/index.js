
// 예상치 못한 에러 처리.
process.on('uncaughtException', (err) => {
    console.log('예기치 못한 에러', err);
});

// TCP server.
const net = require("net");
const server = net.createServer();
server.listen(9000, function () {
    console.log("server listening to 9000 port, %j", server.address());
});

// 문자열 바이트 길이 구하는 함수.
function strByteLength(s, b, i, c) {
    for (b = i = 0; c = s.charCodeAt(i++); b += c >> 11 ? 3 : c >> 7 ? 2 : 1);
    return b
}

// 본문.
var clients = {};

server.on("connection", function (socket) {
    try {
        let date = new Date();
        console.log("new client ", date);

        var recvData = Buffer.alloc(0);
        socket.on("data", function (data) {
            try {
                let Data = new Object();
                var dataStrings = data.toString().split("Partition");
                
                for (var username in clients) { // 다른 유저들에게 전송.
                    if (clients[username] != socket) {
                        clients[username].write(data.toString());
                    }
                }

                var arr = [recvData, data];
                recvData = Buffer.concat(arr);
                if(recvData.byteLength > 4){
                    var byteCount = recvData.toString("utf8",0,4);

                    if(recvData.byteLength >= 4 + parseInt(byteCount)){
                        var originData = recvData.toString("utf8",4, 4 + parseInt(byteCount));

                        HandleData(originData);

                        recvData = recvData.slice(4 + parseInt(byteCount),recvData.byteLength);
                    }
                }
                
            } catch (error) {
                let date = new Date();
                console.error(date, ":", error);
            }
        });

        function HandleData(dataStrings) {
            var data = dataStrings.split("Partition");

            for (var index in data) {
                if (data[index] == "")
                    continue;

                var Data = JSON.parse(data[index]);

                if (Data.eventName == "Connection") {
                    if (clients.hasOwnProperty(Data.userName)) {
                        socket.destroy();
                        return;
                    }

                    clients[Data.userName] = socket;
                    console.log(clients);
                }
            }
        }

        socket.once("close", function (c) {
            try {
                var disconUserName = "";
                for (var username in clients) {
                    if (clients[username] == socket) {
                        disconUserName = username;
                    }
                }
                if(disconUserName == "")
                    return;

                var json = {
                    userName : disconUserName,
                    eventName : "Disconnection",
                    eventData : {}
                };

                var sendMsg = JSON.stringify(json)+ "Partition";

                var lngBuf = Buffer.alloc(4);
                var lng = strByteLength(sendMsg);

                lngBuf.write(lng+"","utf8");

                delete clients[disconUserName];
                console.log(clients);

                for (var username in clients) { // 다른 유저들에게 전송.
                    if (clients[username] != socket) {
                        clients[username].write(lngBuf.toString() + sendMsg);
                    }
                }

                let date = new Date();
                console.log("closed ", date);

            } catch (error) {
                let date = new Date();
                console.error(date, ":", error);
            }
        });

    } catch (error) {
        let date = new Date();
        console.error(date, ":", error);
    }
});