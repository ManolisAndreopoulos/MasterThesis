using System;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;

//using System.Runtime.Serialization.Formatters.Binary;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class TCPClient : MonoBehaviour
{
    #region Unity Functions

    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
#if WINDOWS_UWP
            StopCoonection();
#endif
        }
    }
    #endregion // Unity Functions

    [SerializeField]
    string hostIPAddress, port;
    public TextMeshPro DebuggingText = null;

    private bool connected = false;
    public bool Connected
    {
        get { return connected; }
    }

    bool lastMessageSent = true;

#if WINDOWS_UWP
    StreamSocket socket = null;
    public DataWriter dw;
    public DataReader dr;
    private async void StartCoonection()
    {
        if (socket != null)
        {
            socket.Dispose();
        }
        DebuggingText.text = "Connecting to " + hostIPAddress + "\n";
        try
        {
            socket = new StreamSocket();
            var hostName = new Windows.Networking.HostName(hostIPAddress);
            await socket.ConnectAsync(hostName, port);
            dw = new DataWriter(socket.OutputStream);
            dr = new DataReader(socket.InputStream);
            dr.InputStreamOptions = InputStreamOptions.Partial;
            connected = true;
            DebuggingText.text += "Connected.\n";
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
    }

    private void StopCoonection()
    {
        dw?.DetachStream();
        dw?.Dispose();
        dw = null;

        dr?.DetachStream();
        dr?.Dispose();
        dr = null;

        socket?.Dispose();
        connected = false;
    }
    public async void SendAbImageBufferAsync(ushort[] data)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("i"); // header "i" for image 

            // Write point cloud
            dw.WriteInt32(data.Length);
            dw.WriteBytes(UINT16ToBytes(data));

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
        lastMessageSent = true;
    }

    public async void SendDepthMapBufferAsync(ushort[] data)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("d"); // header "d" for depth

            // Write point cloud
            dw.WriteInt32(data.Length);
            dw.WriteBytes(UINT16ToBytes(data));

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
        lastMessageSent = true;
    }

    //Not Working
    public async void SendImageAsync(byte[] abImage, byte[] depthImage)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("d"); // header "d" for depth camera

            // Write image
            dw.WriteInt32(abImage.Length + depthImage.Length);
            dw.WriteBytes(abImage);
            dw.WriteBytes(depthImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
        lastMessageSent = true;
    }

    public async Task<bool> SendUINT16Async(ushort[] data1, ushort[] data2)
    {
        if (!lastMessageSent) return false;
        lastMessageSent = false;
        try
        {
            using(var dw = new DataWriter(socket.OutputStream))
            {
                // Write header
                dw.WriteString("s"); // header "s" stands for it is ushort array (uint16)

                // Write Length
                dw.WriteInt32(data1.Length + data2.Length);

                // Write actual data
                dw.WriteBytes(UINT16ToBytes(data1));
                dw.WriteBytes(UINT16ToBytes(data2));

                // Send out
                await dw.StoreAsync();
                await dw.FlushAsync();
                dw.DetachStream();
            } 
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
        lastMessageSent = true;
        return true;
    }

    public async void SendSpatialImageAsync(byte[] LRFImage, long ts_left, long ts_right)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        try
        {
            // Write header
            dw.WriteString("f"); // header "f"

            // Write Timestamp and Length
            dw.WriteInt32(LRFImage.Length);
            dw.WriteInt64(ts_left);
            dw.WriteInt64(ts_right);

            // Write actual data
            dw.WriteBytes(LRFImage);

            // Send out
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            DebuggingText.text += webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message;
            DebuggingText.text += "\n";
        }
        lastMessageSent = true;
    }

#endif


    #region Helper Function
    byte[] UINT16ToBytes(ushort[] data)
    {
        byte[] ushortInBytes = new byte[data.Length * sizeof(ushort)];
        System.Buffer.BlockCopy(data, 0, ushortInBytes, 0, ushortInBytes.Length);
        return ushortInBytes;
    }
    #endregion

    #region Button Callback
    public void ConnectToServerEvent()
    {
#if WINDOWS_UWP
        if (!connected) StartCoonection();
        else StopCoonection();
#endif
    }
    #endregion
}
