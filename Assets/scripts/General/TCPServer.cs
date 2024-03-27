using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class PROCESS_CODES{
    public const byte NO_REPLY 	  = 0b0000_0000;
    public const byte SIMCB_REPLY = 0b1000_0000;
    public const byte UNITY_REPLY = 0b0100_0000;
    public const byte SIM_DATA    = 0b0010_0000;
    public const byte SIM_WDGF    = 0b0001_0000;
    public const byte ACK_REPLY   = 0b0000_0001;
    public const byte WDGS_REPLY  = 0b0000_0010;
    public const byte CHANGED_REPLY = 13;
    
}

public class CommandPacket{
    public byte[] body; // include id, body, crc
    public int body_counter;
    public bool EscapeNextByte;
    public int bodyLen;
    private const byte ESCAPE = 255;
    private const byte START = 253;
    private const byte END = 254;
    public SceneManagement sceneManagement;
    public bool log = true;
    public enum states : int {
        Waiting = 0,
        Reading = 1,
    }
    public states commandState;
    // write to this
    public CommandPacket(int length, SceneManagement sceneM){
        commandState = states.Waiting;
        bodyLen = length;
        body = new byte[bodyLen];
        body_counter = 0;
        EscapeNextByte = false;
        sceneManagement = sceneM;
    }
    // process from this
    public CommandPacket(SceneManagement sceneM, byte[] bytedata, int length){
        commandState = states.Waiting;
        bodyLen = length;
        body = new byte[length];
        System.Buffer.BlockCopy(bytedata, 0, body, 0, length);
        body_counter = 0;
        EscapeNextByte = false;
        sceneManagement = sceneM;
    }
    // send from this
    public CommandPacket(SceneManagement sceneM, 
                        ushort id, 
                        byte[] header, 
                        byte[] data){
        commandState = states.Waiting;
        bodyLen = 2 + header.Length + data.Length + 2;
        body = new byte[bodyLen];
        
        byte[] id_bytes = System.BitConverter.GetBytes(id);
        
        System.Array.Reverse(id_bytes);
        //Debug.Log("create command with id: " + System.BitConverter.ToUInt16(new byte[]{id_bytes[1], id_bytes[0]}, 0));
        System.Buffer.BlockCopy(id_bytes, 0, body, 0, 2);
        System.Buffer.BlockCopy(header, 0, body, 2, header.Length);
        System.Buffer.BlockCopy(data, 0, body, 2 + header.Length, data.Length);
        System.Buffer.BlockCopy(crc_itt16_false(2 + header.Length + data.Length), 0, body, 2 + header.Length + data.Length, 2);
        //Debug.Log("New Send Packet Created");
        body_counter = 0;
        EscapeNextByte = false;
        sceneManagement = sceneM;
    }
    public byte[] crc_itt16_false(int length){
        ushort crc = 0xFFFF;
        int pos = 0;
        while (pos < length){
            byte b = body[pos];
            for (int i = 0; i < 8; i++){
                int bit = ((b >> (7-i) & 1) == 1) ? 1 : 0;
                int c15 = ((crc >> 15 & 1) == 1) ? 1 : 0;
                crc <<= 1;
                crc &= 0xFFFF;
                if (c15 != bit){
                    crc ^= 0x1021;
                }
            }
            pos += 1;
        }
        byte[] crc_bytes = System.BitConverter.GetBytes(crc);
        System.Array.Reverse(crc_bytes);
        return crc_bytes;
    }
    public bool processByte(byte data){
        //Debug.Log("Byte: " + data + ", Count: " + body_counter + ", bodyLen: " + bodyLen + ", Escape: " + EscapeNextByte);
        switch(commandState){
            case states.Reading:
                // buffer overflow (increase bodyLen or endByte missed)
                if (body_counter >= bodyLen){
                    Debug.Log("Command length overflown");
                    return true;
                }
                // terminate message
                if (data == END) {
                    if (EscapeNextByte) {
                        body[body_counter] = data;  // also load the end byte
                        body_counter += 1;
                        EscapeNextByte = false;
                    }
                    else {
                        //Debug.Log("Count: " + body_counter);
                        byte[] expected_crc = crc_itt16_false(body_counter-2);
                        if (expected_crc[0] != body[body_counter-2] && expected_crc[1] != body[body_counter-1]) {
                            Debug.LogWarning("Expected CRC: " + System.String.Join(',', expected_crc));
                        }
                        return true;
                    }
                    break;
                }
                // escape next byte
                else if (data == ESCAPE && !EscapeNextByte) {
                    EscapeNextByte = true; 
                    break;
                }
                // load data
                body[body_counter] = data;
                //Debug.Log("Add byte to body: " + body[body_counter]);
                body_counter += 1;
                EscapeNextByte = false;
                break;
            case states.Waiting:
                if (data == START){
                    commandState = states.Reading;
                }
                break;
            //case states.WaitProcess:
            //    processCommand();
            //    commandState = states.Processed;
            //    break;
        }
        return false;
    }
    private bool isCommand(byte[] command){
        int index = 0;
        while (index < command.Length) {
            if (body[index + 2] != command[index]){
                return false;
            }
            index += 1;
        }
        return true;
    }
    public byte processCommand(Dictionary<string, byte[]> commands){
        byte process_code = PROCESS_CODES.NO_REPLY;
        //Debug.Log("Length: " + body.Length + " Bytes: " + ToString());
        bool is_command = false;
        foreach (KeyValuePair<string, byte[]> command in commands) {
            if (isCommand(command.Value)) {
                is_command = true;
                Debug.Log("Received Command: " + command.Key + " ID: " + System.BitConverter.ToUInt16(new byte[] {body[1], body[0]}, 0));
                if (System.BitConverter.ToUInt16(new byte[] {body[1], body[0]}, 0) < 20000){
                    sceneManagement.allCommandsReceived.Add(command.Key);
                }
                switch(command.Key){
                    case "SIMSTAT":
                        //Debug.Log("SIMSTAT: " + ToString());
                        float[] motor_power = ParseFloats(body, 8, 9);
                        //Debug.Log("Vals: " + string.Join(",", motor_power) + " Mode: " + body[body.Length-4] + " WDog Status: " + body[body.Length-3]);
                        if (body[body.Length-3] == 1){
                            process_code = PROCESS_CODES.SIM_DATA;
                            break;
                        }
                        sceneManagement.setMotorPower(  motor_power[0],motor_power[1],motor_power[2],motor_power[3],
                                                        motor_power[4],motor_power[5],motor_power[6],motor_power[7]);
                        process_code = PROCESS_CODES.SIM_DATA;
                        //process_code |= PROCESS_CODES.SIM_WDGF;
                        break;
                    case "ACK":
                        if(body[7] != 0) {
                            Debug.LogWarning("Received problematic ACK message from CB. ID: " 
                                + System.BitConverter.ToUInt16(new byte[] {body[6], body[5]}, 0) 
                                + " Code: " + body[7]);
                            Debug.Log("Bytes:" + ToString());
                        }
                        //Debug.Log("ID: " 
                        //        + System.BitConverter.ToUInt16(new byte[] {body[6], body[5]}, 0) 
                        //        + " Code: " + body[7]);
                        
                        // if <id> is from rust to cb, reply rust with ack, else from simulator, no reply
                        if (System.BitConverter.ToUInt16(new byte[] {body[6], body[5]}, 0) < 60000){
                            process_code = PROCESS_CODES.ACK_REPLY;
                        } else {process_code = PROCESS_CODES.NO_REPLY;}
                        //Debug.Log("ACK: " + ToString());
                        break;
                    case "WDGS":
                        process_code = PROCESS_CODES.WDGS_REPLY;
                        //Debug.Log("WDGS: " + ToString());
                        break;
                    case "SASSISTTN":
                        int newLength = body.Length-4;
                        byte[] temp = new byte[newLength];
                        System.Buffer.BlockCopy(body, 0, temp, 0, 2);
                        temp[2] = 80; // 'P'
                        temp[3] = 73; // 'I'
                        temp[4] = 68; // 'D'
                        System.Buffer.BlockCopy(body, 9, temp, 5, newLength-5);
                        Debug.Log("original: " + ToString());
                        body = temp;
                        System.Buffer.BlockCopy(crc_itt16_false(newLength-2), 0, body, newLength-2, 2);
                        Debug.Log("transformed: " + ToString());
                        process_code = PROCESS_CODES.CHANGED_REPLY;
                        break;
                    // simulation environment commands
                    case "SIMCBTOGU":
                        bool _simcb_connect = true;
                        if (body[11] == 0b0000_0000) {
                            _simcb_connect = false;
                        }
                        sceneManagement.simCBRefresh = true;
                        sceneManagement.simCBToggle = _simcb_connect;
                        process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "CAPTUREU":
                        if (sceneManagement.getCameraMode() == body[10]){
                            sceneManagement.captureImage(body[10]);
                        } else {
                            sceneManagement.configRobotCamera(mode : body[10]);
                            sceneManagement.captureImage(body[10]);
                        }
                    	process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "CAMCFGU":
                        int cheight = System.BitConverter.ToInt32(body, 9);
                        int cwidth = System.BitConverter.ToInt32(body, 13);
                        sceneManagement.camCFG[0] = cheight;
                        sceneManagement.camCFG[1] = cwidth;
                        sceneManagement.camCFG[2] = body[17];
                        sceneManagement.camCFG_Effects = body[18];
                        if ((body[18] & 0b1000_0000) != 0){
                            sceneManagement.rgbScreenResizeToggle = true;
                        } else {
                            sceneManagement.rgbScreenResizeToggle = false;
                        }
                        if ((body[18] & 0b0100_0000) != 0){
                            sceneManagement.ShowGUIToggle = true;
                        } else {
                            sceneManagement.ShowGUIToggle = false;
                        }
                        sceneManagement.camCFGRefresh = true;
                        process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "SETENVU":
                        sceneManagement.sceneSelect = body[9];
                        sceneManagement.sceneRefresh = true;
                        //sceneManagement.ResetScene();
                        process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "ROBOTSELU":
                    	process_code = PROCESS_CODES.UNITY_REPLY;
                        goto default;
                    case "ROBCFGU":
                        float[] rob_cfg = ParseFloats(body, 6, 9);
                        Debug.Log("Received CFG: " + System.String.Join(',', rob_cfg));
                        sceneManagement.robotCFG[0] = rob_cfg[0];
                        sceneManagement.robotCFG[1] = rob_cfg[1]; 
                        sceneManagement.robotCFG[2] = rob_cfg[2]; 
                        sceneManagement.robotCFG[3] = rob_cfg[3]; 
                        sceneManagement.robotCFG[4] = rob_cfg[4]; 
                        sceneManagement.robotCFG[5] = rob_cfg[5];
                        sceneManagement.robotCFGRefresh = true;
                        process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "SCECFGU":
                        sceneManagement.sceneToggles = body[9];
                        sceneManagement.scatterColorBias = System.BitConverter.ToInt16(body, 10);
                        Debug.Log("bias: " + sceneManagement.scatterColorBias);
                        sceneManagement.sceneTogglesRefresh = true;
                        process_code = PROCESS_CODES.UNITY_REPLY;
                        break;
                    case "HEARTBEAT":
                        process_code = PROCESS_CODES.NO_REPLY;
                        break;
                    default:
                        Debug.Log("Unimplemented command: " + command.Key);
                        break;
                }
            }
            if (is_command) {
                break;  //break for loop if command is found
            }
        }
        if (!is_command) {
            Debug.Log("Unknown Command (sending to simCB if possible). Bytes: " + ToString());
            sceneManagement.allCommandsReceived.Add("unCom: " + ToString());
        }
        
        //ParseFloats(body,1,0);
        //Debug.Log(ParseFloats(body,1,0)[0]);
        return process_code;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nwStream"></param>
    /// <param name="bytes_to_send"></param>
    /// <returns> 
    /// True: END byte sent
    /// False: END byte not sent
    /// </returns>
    public bool sendPacket(NetworkStream nwStream, int bytes_to_send){
        while (bytes_to_send > 0){
            if (!nwStream.CanWrite || !nwStream.CanRead){
                return true;
            }
            bytes_to_send -= 1;
            //Debug.Log("count: " + body.Length + " i: " + body_counter);
            if (body_counter == 0) {
                nwStream.Write(new byte[] {START}, 0 ,1);
                //Debug.Log("Wrote START");
            }
            //Debug.Log(body_counter + ", " + body.Length);
            byte writebyte = body[body_counter];
            
            //Debug.Log("write byte" + writebyte);
            if (writebyte == START || writebyte == ESCAPE || writebyte == END) {
                nwStream.Write(new byte[] {ESCAPE, writebyte}, 0, 2);
                //Debug.Log("Wrote ESCAPE + byte");
            } else {
                nwStream.Write(new byte[] {writebyte}, 0, 1);
                //Debug.Log("Wrote byte");
            }

            body_counter += 1;
            if (body_counter == body.Length) {
                //Debug.Log("Sent: " + ToString());
                nwStream.Write(new byte[] {END}, 0 ,1);
                //Debug.Log("Wrote END");
                return true;
            }
        }
        return false;
    }
    public void reset(){
        System.Array.Clear(body, 0, bodyLen);
        body_counter = 0;
        EscapeNextByte = false;
        commandState = states.Waiting;
    }
    public override string ToString(){
        if (log){
            //byte[] no_header_crc_body = new byte[body.Length-4];
            //System.Array.Copy(body, 2, no_header_crc_body, 0, no_header_crc_body.Length);
            //return System.String.Join(",", no_header_crc_body);
            return System.String.Join(",", body);
        }
        return "";
        //return System.Text.Encoding.Default.GetString(body);
    }
    public static float[] ParseFloats(byte[] byteValues, int numFloats, int startIndex){
        float[] floatValues = new float[numFloats];
        int count = 0;
        while (count < numFloats){
            //Debug.Log(byteValues[0]+","+byteValues[1]+","+byteValues[2]+","+byteValues[3]+",");
            floatValues[count] = System.BitConverter.ToSingle(byteValues, startIndex);
            startIndex += 4;
            count += 1;
        }
        return floatValues;
    }
}
public class TCPServer : MonoBehaviour
{
    readonly string[] POSSIBLE_COMMANDS = new string[] {
            // control board commands
            "RAW", "LOCAL", "GLOBAL", "RELDOF", "BNO055P", "MS5837P", "WDGS", "BNO055R", "MS5837R",
            "MMATS", "MMATU", "TINV", "BNO055A", "WDGF", "SASSIST2",
            // other commands
            "SASSISTTN",
            // unity commands
            "CAPTUREU", "SETENVU", "CAMCFGU", "ROBOTSELU", "RANDENVU", "SIMCBTOGU", "ROBCFGU","SCECFGU",
            // acknowledge
            "ACK",
            // simCB
            "HEARTBEAT", "SIMHIJACK", "SIMDAT", "SIMSTAT"
        };
    public int receive_buffer_size = 8;
    Dictionary<string, byte[]> commandsHeader = new Dictionary<string, byte[]>();
    SceneManagement sceneManagement;
    public string IPAddr = "127.0.0.1";
    public int port = -1;
    public float msPerTransmit = 0.1f;
    private float currentTime = 0;
    public string ui_message = "Closed";
    private Thread threadRust;
    public bool runServer = false;
    private bool serverStarted = false;
    private byte[] buffer;
    List<CommandPacket> receiveCommandsPool = new List<CommandPacket>(64);
    List<CommandPacket> sendCommandsPool = new List<CommandPacket>(64);
    CommandPacket receiveLoadingPacket;
    CommandPacket sendLoadingPacket;

    private Thread threadSimCB;
    public bool simCB_Connect = false;
    //public string simCB_IPAddr = "172.27.160.1";
    public int simCB_Port = 5014;
    public bool simCB_Connected = false;
    TcpClient simCB_client;
    private byte[] simCB_buffer;
    public ushort simCB_ID = 60000;
    public float simCB_currentTime = 0;
    public float simCB_imu_currentTime = 0;
    public const float SIMCB_IMU_INTERVAL = 0.1f; // every N second send one imu packet 
    List<CommandPacket> simCB_receiveCommandsPool = new List<CommandPacket>(64);
    List<CommandPacket> simCB_sendCommandsPool = new List<CommandPacket>(64);
    CommandPacket simCB_receiveLoadingPacket;
    CommandPacket simCB_sendLoadingPacket;
    void Start()
    {
        foreach (string command in POSSIBLE_COMMANDS) {
            commandsHeader[command] = Encoding.UTF8.GetBytes(command);
        }
        Debug.Log(commandsHeader.Keys.Count);
        sceneManagement = GetComponent<SceneManagement>();
        receiveLoadingPacket = new CommandPacket(128, sceneManagement);
        sendLoadingPacket = new CommandPacket(128, sceneManagement);
        buffer = new byte[receive_buffer_size];
        

        simCB_buffer = new byte[receive_buffer_size];
        simCB_receiveLoadingPacket = new CommandPacket(128, sceneManagement);
        simCB_sendLoadingPacket = new CommandPacket(128, sceneManagement);
        
    }
    void Update(){
        if (serverStarted) {
            currentTime += Time.deltaTime * 1000;
        }
        if (simCB_Connected) {
            simCB_currentTime += Time.deltaTime * 1000;
            simCB_imu_currentTime += Time.deltaTime;
        }
        if (!serverStarted && runServer) {
            startRustThread();
            serverStarted = true;
        }
        if (serverStarted && !runServer) {
            stopRustThread();
            serverStarted = false;
        }
        //Debug.Log("Received Count: " + receiveCommandsPool.Count + " Send Count: " + sendCommandsPool.Count);
        //print(runServer);

        if (simCB_Connect && !simCB_Connected) {
            Debug.Log("SimCB connect");
            simCB_client = new TcpClient("127.0.0.1", simCB_Port);
            //simCB_client.ReceiveBufferSize = receive_buffer_size;
            simCB_Connected = true;
            startSimCBThread();
            Debug.Log("SimCB connected");
        }
        if (!simCB_Connect && simCB_Connected) {
            Debug.Log("SimCB disconnect");
            simCB_Connected = false;
            stopSimCBThread();
            Debug.Log("SimCB disconnected");
        }
    }
    void OnDestroy(){
        stopRustThread();
        stopSimCBThread();
    }
    void OnApplicationQuit(){
        stopRustThread();
        stopSimCBThread();
    }
    void SimHijack(ushort simCB_id = 60000, byte hijack = 1){
        simCB_sendCommandsPool.Add(
                            new CommandPacket(sceneManagement, 
                                            id: simCB_id,
                                            header: commandsHeader["SIMHIJACK"],
                                            data: new byte[] {hijack})
                                    );
    }
    void SimData(IMU imu, ushort simCB_id = 60000){
        byte[] simCB_imu_data = new byte[20];
        //Debug.Log("Little Endian: " + System.BitConverter.IsLittleEndian);
        //Debug.Log("IMU (xyzw): " + imu.robotRotation + " Depth: " + imu.robotPosition.z);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(
                                imu.robotRotation.w), 0, simCB_imu_data, 0, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(
                                imu.robotRotation.x), 0, simCB_imu_data, 4, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(
                                imu.robotRotation.y), 0, simCB_imu_data, 8, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(
                                imu.robotRotation.z), 0, simCB_imu_data, 12, 4);
        System.Buffer.BlockCopy(System.BitConverter.GetBytes(
                                imu.robotPosition.z), 0, simCB_imu_data, 16, 4);
        simCB_sendCommandsPool.Add(
                            new CommandPacket(sceneManagement,
                                            id: simCB_id,
                                            header: commandsHeader["SIMDAT"],
                                            data: simCB_imu_data));
        simCB_sendCommandsPool[simCB_sendCommandsPool.Count-1].log = false;
    }
    void GetData(int port)
    {
        if (port < 0){
            return;
        }
        
        TcpListener server = new TcpListener(IPAddress.Parse(IPAddr), port);
        server.Start();
        ui_message = "Waiting... :"+IPAddr+":"+port;
        Debug.Log("Port:" + port + ", started on " + IPAddr + ". waiting connection...");
        while (!server.Pending()) {
            if (!runServer) {
                server.Stop();
                ui_message = "Closed:"+IPAddr+":"+port;
                Debug.Log("Port:" + port + ", closed on " + IPAddr);
                return;
            }
        }
        // Create a client to get the data stream
        TcpClient client = server.AcceptTcpClient();

        // TODO: FIND BETTER MESSAGE PROCESSING METHODS SO THIS NUMBER CAN BE AS LOW AS POSSIBLE
        client.ReceiveBufferSize = receive_buffer_size;  

        NetworkStream nwStream = client.GetStream();
        ui_message = "Connected:"+IPAddr+":"+port;
        Debug.Log("Port:"+port + ", stream connected");
        // Start listening
        bool running;
        while (runServer)
        {
            try{
                running = Connection(client, nwStream, false);
            }catch (System.Exception e){
                serverStarted = false;
                Debug.LogWarning("Connection Exception, closing connection: " + e);
                break;
            }
        }
        Debug.Log("Port:" + port + ", closed on " + IPAddr);
        server.Stop();
        ui_message = "Closed:"+IPAddr+":"+port;
        // Create the server
        
    }
    void GetDataSimCB(){
        NetworkStream nwStream = simCB_client.GetStream();
        SimHijack();
        simCB_ID += 1;
        while (simCB_Connected)
        {
            Connection(simCB_client, nwStream, true);
        }
    }
    bool Connection(TcpClient client, NetworkStream nwStream, bool isSimCB)
    {
        //Debug.Log(currentTime);
        if (isSimCB) {
            //Debug.Log(simCB_currentTime);
            if (simCB_currentTime > msPerTransmit && simCB_sendCommandsPool.Count > 0) {
                ParseSendData(nwStream, isSimCB);
                simCB_currentTime = 0;
            }
        } else {
            if (currentTime > msPerTransmit && sendCommandsPool.Count > 0) {
                ParseSendData(nwStream, isSimCB);
                currentTime = 0;
            }
        }
        // Read data from the network stream
        if (nwStream.DataAvailable){
            int bytesRead = 0;
            
            if (isSimCB){
                bytesRead = nwStream.Read(simCB_buffer, 0, simCB_buffer.Length);
            } else {
                bytesRead = nwStream.Read(buffer, 0, buffer.Length);
            }
            //print(bytesRead);
            ParseReceivedData(bytesRead, isSimCB);
        }
        else{
            if (isSimCB){
                if (simCB_receiveCommandsPool.Count > 0) {
                    if (simCB_receiveCommandsPool.Count > 128) {
                        print("simCB: TCP Receive Command Pool Overflow");
                        simCB_receiveCommandsPool.RemoveRange(0, simCB_receiveCommandsPool.Count / 2);
                    }
                    //Debug.Log("simCB: processing command");
                    byte process_code = simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].processCommand(commandsHeader);
                    process_code = (byte)(process_code | PROCESS_CODES.SIMCB_REPLY);
                    switch (process_code) {
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.ACK_REPLY):
                            Debug.Log("simCB replying to Rust with ACK: " + simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].ToString());
                            sendCommandsPool.Add(
                                new CommandPacket(sceneManagement, 
                                                simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body,
                                                simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body.Length
                                                ));
                            break;
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.WDGS_REPLY):
                            Debug.Log("simCB replying to Rust with WDGS, " + simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].ToString());
                            sendCommandsPool.Add(
                                new CommandPacket(sceneManagement, 
                                                simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body,
                                                simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body.Length
                                                ));
                            break;
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.SIM_DATA):
                            if(simCB_sendCommandsPool.Count < 1){
                                Debug.Log("simCB robot IMU packet");
                                SimData(sceneManagement.getRobotIMU(), simCB_ID);
                                simCB_ID += 1;
                                if (simCB_ID > 65000) {simCB_ID = 60000;}
                            }
                            if (simCB_imu_currentTime > SIMCB_IMU_INTERVAL){ 
                                simCB_imu_currentTime = 0;
                            }
                            break;
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.NO_REPLY):
                            break;
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.UNITY_REPLY):
                            Debug.Log("Processed command for Unity");
                            
                            break;
                        case (PROCESS_CODES.SIMCB_REPLY | PROCESS_CODES.CHANGED_REPLY):
                            simCB_sendCommandsPool.Add(new CommandPacket(sceneManagement,
                                                        simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body,
                                                        simCB_receiveCommandsPool[simCB_receiveCommandsPool.Count-1].body.Length));
                            break;
                        default:
                            break;
                    }
                    simCB_receiveCommandsPool.RemoveAt(simCB_receiveCommandsPool.Count-1);
                }
            } else {
            // process the latest received command (Last in first out)
                if (receiveCommandsPool.Count > 0){
                    // received too many commands, clear oldest half of commands
                    if (receiveCommandsPool.Count > 128) {
                        print("TCP Receive Command Pool Overflow");
                        receiveCommandsPool.RemoveRange(0, receiveCommandsPool.Count / 2);
                    }

                    if(simCB_Connected){
                        // SimCB
                        // copy command to send to simCB
                        // Debug.Log("Copying rust command to simCB send pool");
                        byte process_code = receiveCommandsPool[receiveCommandsPool.Count-1].processCommand(commandsHeader);
                        switch (process_code) {
                            case (PROCESS_CODES.UNITY_REPLY):
                                Debug.Log("Processed command for Unity");
                                sendCommandsPool.Add(new CommandPacket(sceneManagement,
                                    id: 0,
                                    header:commandsHeader["ACK"],
                                    data: new byte[] {receiveCommandsPool[receiveCommandsPool.Count-1].body[0],
                                                      receiveCommandsPool[receiveCommandsPool.Count-1].body[1],
                                                      0}
                                    ));
                                break;
                            default:
                                Debug.Log("Copying rust command to simCB send pool");
                                simCB_sendCommandsPool.Add(
                                new CommandPacket(sceneManagement, 
                                                receiveCommandsPool[receiveCommandsPool.Count-1].body,
                                                receiveCommandsPool[receiveCommandsPool.Count-1].body.Length)
                                                );
                                break;
                        }
                        
                    } else {
                        // Rust only
                        // TODO: Modify this for asking sim cb connection and inital startup
                        byte process_code = receiveCommandsPool[receiveCommandsPool.Count-1].processCommand(commandsHeader);
                        switch (process_code) {
                            case (PROCESS_CODES.UNITY_REPLY):
                                Debug.Log("Processed command for Unity");
                                sendCommandsPool.Add(new CommandPacket(sceneManagement,
                                    id: 0,
                                    header:commandsHeader["ACK"],
                                    data: new byte[] {receiveCommandsPool[receiveCommandsPool.Count-1].body[0],
                                                      receiveCommandsPool[receiveCommandsPool.Count-1].body[1],
                                                      0}
                                    ));
                                break;
                            default:
                                Debug.LogWarning("Received SimCB Command but SimCB is not connected");
                                break;
                        }
                    }
                    receiveCommandsPool.RemoveAt(receiveCommandsPool.Count-1);
                }
            }
        }
        
            //Debug.Log("No msg received");
        return true;
    }
    void ParseSendData(NetworkStream nwStream, bool isSimCB, bool log = true){
        if (isSimCB) {
            if (simCB_sendCommandsPool.Count > 128) {
                Debug.LogWarning("simCB: TCP Send Command Pool Overflow");
                simCB_sendCommandsPool.RemoveRange(1, simCB_sendCommandsPool.Count / 2);
            }
            if (simCB_Connected) {
                //Debug.Log("Sending simCB packet");
                if (simCB_sendCommandsPool[0].sendPacket(nwStream, 6)) {
                    if (log){
                        Debug.Log("Sent simCB: " + simCB_sendCommandsPool[0].ToString());
                    }
                    simCB_sendCommandsPool.RemoveAt(0);
                };
                return;
            }
            return;
        }
        // have too many sends in the pool, clear the oldest half of commands (but leave zero index for sending)
        if (sendCommandsPool.Count > 128) {
            Debug.LogWarning("TCP Send Command Pool Overflow");
            sendCommandsPool.RemoveRange(1, sendCommandsPool.Count / 2);
        }
        // process the earlies send command (Last in last out)
        // because new send may be added to the pool
        Debug.Log(sendCommandsPool.Count + nwStream.ToString());
        if (sendCommandsPool[0].sendPacket(nwStream, 6)) {
            Debug.Log("Complete Send to Rust");
            sendCommandsPool.RemoveAt(0);
        }
    }
    // Use-case specific function, need to re-write this to interpret whatever data is being sent
    void ParseReceivedData(int bytesRead, bool isSimCB)
    {
        int i = 0;
        if (isSimCB){
            // SimCB
            while (i < bytesRead){
                if (simCB_receiveLoadingPacket.processByte(simCB_buffer[i])){
                    //print(simCB_receiveLoadingPacket.ToString());
                    Debug.Log("SimCB received command packet");
                    CommandPacket newPacket = new CommandPacket(sceneManagement, simCB_receiveLoadingPacket.body, simCB_receiveLoadingPacket.body_counter);
                    simCB_receiveCommandsPool.Add(newPacket);
                    simCB_receiveLoadingPacket.reset();
                }
                //print("received");
                i += 1;
            }
            return;
        } else {
            // Robot (Rust)
            while (i < bytesRead){
                if (receiveLoadingPacket.processByte(buffer[i])){
                    //print(receiveLoadingPacket.ToString());
                    Debug.Log("RustServer received command packet");
                    CommandPacket newPacket = new CommandPacket(sceneManagement, receiveLoadingPacket.body, receiveLoadingPacket.body_counter);
                    receiveCommandsPool.Add(newPacket);
                    receiveLoadingPacket.reset();
                }
                //print("received");
                i += 1;
            }
        }
    }

    void startRustThread(){
        threadRust = new Thread(() => GetData(port));
        threadRust.Start();
    }
    void startSimCBThread(){
        threadSimCB = new Thread(() => GetDataSimCB());
        threadSimCB.Start();
    }
    void stopRustThread(){
        runServer = false;
        threadRust.Join();
        receiveCommandsPool.Clear();
        sendCommandsPool.Clear();
        threadRust = new Thread(() => GetData(port));
    }
    void stopSimCBThread(){
        simCB_Connected = false;
        threadSimCB.Join();
        simCB_client.Close();
        simCB_receiveCommandsPool.Clear();
        simCB_sendCommandsPool.Clear();
        threadSimCB = new Thread(() => GetDataSimCB());
    }
    
    // Position is the data being received in this example
    //Vector3 position = Vector3.zero;
    //void Update()
    //{
    //    // Set this object's position in the scene according to the position received
    //    //transform.position = position;
    //}
}
