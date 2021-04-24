
// 예상치 못한 에러 처리.
process.on('uncaughtException', (err) => {
    console.log('예기치 못한 에러', err);
});

// TCP server.
const net = require("net");
const { stringify } = require("querystring");
const server = net.createServer();
server.listen(9000, function () {
    console.log("server listening to 9000 port, %j", server.address());
});


// 본문.
var clients = {};

server.on("connection", function (socket) {
    try {
        let date = new Date();
        console.log("new client ", date);

        socket.on("data", function (data) {
            try {
                let Data = new Object();
                var dataStrings = data.toString().split("Partition");
                
                for (var username in clients) { // 다른 유저들에게 전송.
                    if (clients[username] != socket) {
                        clients[username].write(data.toString());
                    }
                }

                for (var index in dataStrings) {
                    if(dataStrings[index] == "")
                        continue;

                    Data = JSON.parse(dataStrings[index]);

                    if(Data.eventName == "Connection"){
                        if(clients.hasOwnProperty(Data.userName)){
                            socket.destroy();
                            return;
                        }

                        clients[Data.userName] = socket;
                        console.log(clients);
                    }
                }
            } catch (error) {
                let date = new Date();
                console.error(date, ":", error);
            }
        });

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

                for (var username in clients) { // 다른 유저들에게 전송.
                    if (clients[username] != socket) {
                        clients[username].write(JSON.stringify(json));
                    }
                }

                delete clients[disconUserName];

                let date = new Date();
                console.log("closed ", date);
                console.log(clients);

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