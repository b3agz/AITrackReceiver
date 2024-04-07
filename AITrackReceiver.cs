using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

public class AITrackReceiver : MonoBehaviour {

    #region Setup Variables, tweak in the inspector to suit.

    [Header("Connection")]
    [Tooltip("The port being used by AITrack. Should be 4242 by default.")]
    [SerializeField] private int _port = 4242;

    [Header("Setup")]
    [Tooltip("Approximate distance (in metres) from your head to the tracking camera.")]
    [SerializeField] private float _distFromCamera = 0.5f;

    [Tooltip("Set to true to have the movements behave like a mirror rather than direct tranlsation.")]
    [SerializeField] private bool _mirror = true;

    [Tooltip("The length of our list of averages. Higher number = a smoother average.")]
    [Range (1, 50)]
    [SerializeField] private int _smoothing = 15;

    #endregion

    // Variables to store the information recieved from AITrack.
    private float _xPos, _yPos, _zPos, _pitch, _yaw, _roll;
    private AvgVector3 _avgPosition, _avgRotation;

    #region Publicly Accessible Variables.

    // The prepared values suitable for applying directly to a transform.
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }

    // The raw information directly out of AITrack
    public Vector3 PositionRaw { get { return new Vector3(_xPos, _yPos, _zPos); } }
    public Quaternion RotationRaw { get { return Quaternion.Euler(_pitch, _yaw, _roll); } }
    public Vector3 EulerRaw { get { return new Vector3(_pitch, _yaw, _roll); } }

    #endregion

    #region Thread Stuff

    private Thread _receiveThread;
    private UdpClient _client;
    private readonly object _positionLock = new object();
    private readonly object _rotationLock = new object();

    #endregion

    /// <summary>
    /// Resets all variables and attempts to start a reciever thread.
    /// </summary>
    private void Init() {

        Debug.Log($"Attempting to initialise UDP Reciever: 127.0.0.1:{_port}");

        // Set our raw values to zero.
        _xPos = _yPos = _zPos = _pitch = _yaw = _roll = 0;

        // Initialise some Averagers for position and rotation.
        _avgPosition = new AvgVector3(_smoothing);
        _avgRotation = new AvgVector3(_smoothing);

        // Belts and braces error check to make sure we haven't already tried to start the thread.
        if (_receiveThread == null) {
            // Setup the thread and start it running.
            _receiveThread = new Thread(new ThreadStart(ReceiveData));
            _receiveThread.IsBackground = true;
            _receiveThread.Start();
        } else {
            Debug.LogWarning("Attempted to start UDP Receiver thread but thread was already running.");
        }
    }

    /// <summary>
    /// Checks to see if we have a thread or client running and aborts/closes them.
    /// </summary>
    private void CloseConnection() {
        if (_receiveThread != null) {
            _receiveThread.Abort();
            _receiveThread = null;
        }
        if (_client != null) {
            _client.Close();
        }
    }

    public void Reset() {
        CloseConnection();
        Init();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) Reset();
        // When not locked, set Position value.
        lock (_positionLock) {

            Vector3 pos = _avgPosition.Value;    // Get the current average position.

            pos.y = -pos.y;                     // Raw Y Pos is upside down so invert it.
            pos.y *= 0.2f;                      // Raw Y Pos is also a lot more sensitive than X so calm it down a bit.

            pos.x = _mirror ? -pos.x : pos.x;   // If mirror is true, invert X pos.

            pos.z = -pos.z;                     // The raw Z position is also inverted so un-invert it.
            pos.z -= (_distFromCamera * 9f);    // Apply the distance from the camera. *9 roughly translates from metres in the real world to Unity Units.
            pos.z *= 8f;                        // Z position barely registers so give it some extra juice.

            pos *= 0.05f;                       // The raw position information is WAY oversized when it arrives in Unity.
            
            Position = pos;

        }

        // When not locked, set the Rotation value.
        lock (_rotationLock) {

            Vector3 rot = _avgRotation.Value;    // Get the current average rotation.
            //rot += _startRotOffset;

            rot.z -= 90f;                       // Z Axis/Roll correction.
            rot.y = _mirror ? -rot.y : rot.y;   // If mirror is true, invert Y Axis/Yaw.
            rot.x *= 4f;                        // Z Axis/Pitch needs a bit of a boost compared to other values.

            Rotation = Quaternion.Euler(rot);

        }

    }

    /// <summary>
    /// Runs continuously checking for infomation from UDP port. DO NOT CALL FROM MAIN THREAD!
    /// </summary>
    private void ReceiveData() {

        Debug.Log($"UDP Reciever thread started for 127.0.0.1:{_port}");

        _client = new UdpClient(_port);

        // Begin UDP Receiver loop.
        while (true) {
            try {

                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = _client.Receive(ref anyIP);

                for (int i = 0; i < 6; i++) {
                    double datum = System.BitConverter.ToDouble(data, i * 8);
                    var j = i + 1;
                    switch (j) {
                        case 1:
                            _xPos = (float)datum;
                            break;
                        case 2:
                            _pitch = (float)datum;
                            break;
                        case 3:
                            _zPos = (float)datum;
                            break;
                        case 4:
                            _yaw = ((float)datum);
                            break;
                        case 5:
                            _yPos = (float)datum;
                            break;
                        case 6:
                            _roll = (float)datum;
                            break;
                    }
                }

                // Add position values to averager.
                lock (_positionLock) {
                    _avgPosition.AddNewPosition(PositionRaw);
                }

                // Add rotation values to average.
                lock (_rotationLock) {
                    _avgRotation.AddNewPosition(EulerRaw);
                }

            } catch (Exception err) {
                Debug.Log(err.ToString());
            }
        }
    }

    /// <summary>
    /// Sets the smoothing value of the position and rotation inputs.
    /// </summary>
    /// <param name="value">The smoothing value, higher = smoother.</param>
    public void SetSmoothingValue(int value) {
        _smoothing = value;
        UpdateSmoothing();
    }

    /// <summary>
    /// Updates the smoothing values of rotation and position.
    /// </summary>
    private void UpdateSmoothing() {
        _avgPosition.SetSize(_smoothing);
        _avgRotation.SetSize(_smoothing);
    }

    #region Automatic Initialisation/Connection Closing

    private void OnEnable() {
        Init();
    }

    private void OnDisable() {
        CloseConnection();
    }

    private void OnApplicationQuit() {
        CloseConnection();
    }

    #endregion

}

/// <summary>
/// Stores a list of Vector3s and returns the average of those Vector3s.
/// </summary>
public class AvgVector3 {

    private List<Vector3> _vectors = new List<Vector3>();
    private int _size = 10;

    public Vector3 Value => CalculateAverage();

    public AvgVector3(int size = 10) {
        SetSize(size);
    }

    /// <summary>
    /// Sets the size of the list that is being averaged. A larger list means a smoother average.
    /// </summary>
    /// <param name="size">List size.</param>
    public void SetSize(int size) {
        _size = size;
        if (_size < 1) _size = 1;
    }

    /// <summary>
    /// Adds a new Vector3 to the list and, if necessary, trims the list to stay within the specified size.
    /// </summary>
    /// <param name="v3">The Vector3 being added.</param>
    public void AddNewPosition(Vector3 v3) {

        // Add the Vector3 to the list.
        _vectors.Add(v3);

        // If list count is higher than size, trim the list from the top down.
        while (_vectors.Count > _size) {
            _vectors.RemoveAt(0);
        }
    }

    /// <summary>
    /// Calculates the average of the list of Vector3s we have stored and returns it.
    /// </summary>
    /// <returns>A Vector3 that is the average of the list.</returns>
    private Vector3 CalculateAverage() {

        // If we haven't put any Vector3s in our list yet, return Vector3.zero.
        if (_vectors == null || _vectors.Count == 0) {
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;

        // Add all of the Vector3s together
        foreach (Vector3 vec in _vectors) {
            sum += vec;
        }

        // Return the sum divided by the number of Vector3s.
        return sum / _vectors.Count;
    }

}
