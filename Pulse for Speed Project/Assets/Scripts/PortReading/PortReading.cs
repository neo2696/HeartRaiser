using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Security.Cryptography;
using UnityEngine;

// System.IO.Ports requires a working Serial Port. On Mac, you will need to purcase the Uniduino plug-in on the Unity Store
// This adds a folder + a file into your local folder at ~/lib/libMonoPosixHelper.dylib
// This file will activate your serial port for C# / .NET
// The functions are the same as the standard C# SerialPort library
// cf. http://msdn.microsoft.com/en-us/library/system.io.ports.serialport(v=vs.110).aspx
public class PortReading : MonoBehaviour
{
    public const string FakePortName = "$fake-port$";
    public const float DisconnectionTimeoutSecs = 10f;
    public const ushort StableInputsToConsiderStableAgain = 7;
    public const float MaxHeartRateChangePerSecond = 50f;

    private int _coroutinesRunning = 0;

    private class PlayerState
    {
        private bool _breakButton;
        private uint _heartRate;
        private long _heartRateSum;
        private long _heartRateNSamples;
        private uint _topHeartRate;
        private bool _recordingStats;
        private float _lastUpdate;

        public uint AverageRate => _heartRateNSamples > 0 ? (uint) (_heartRateSum / _heartRateNSamples) : 0;
        public uint MaxHeartRate => _topHeartRate;

        public float LastUpdate => _lastUpdate;
        public bool IsHeartRateConnected => Time.unscaledTime < _lastUpdate + DisconnectionTimeoutSecs;

        public PlayerState() {
            _lastUpdate = Time.unscaledTime;
        }

        // Break button for each player
        public bool BreakButton {
            get => _breakButton;
            set => _breakButton = value;
        }

        // Heart rate for each player
        public uint Rate {
            get => _heartRate;
            set {
                _heartRate = value;
                _lastUpdate = Time.unscaledTime;
                if (_recordingStats) {
                    _heartRateSum += _heartRate;
                    _heartRateNSamples += 1;
                    if (_heartRate > _topHeartRate) _topHeartRate = _heartRate;
                }
            }
        }

        public void ResetAverageStats() {
            _topHeartRate = 0;
            _heartRateSum = 0;
            _heartRateNSamples = 0;
        }

        /**
         * Be careful! This method resets the stats before recording!
         */
        public void RecordStats() {
            ResetAverageStats();
            _recordingStats = true;
        }

        // It will not erase recorded stats, just stop
        public void StopRecordingStats() {
            _recordingStats = false;
        }
    }

    private interface IPortReader
    {
        bool IsPortOpen { get; }
        string PortName { get; }
        uint LastReceivedHeartRate { get; }
        bool IsHeartRateStable { get; }

        // Should read buffered values and update the associated player states
        void Read();

        // Called on destroying
        void Destroy();

        // Add the managed state to be managed by the reader
        void AddManagedState(PlayerState state);

        // Remove the managed state
        void RemoveManagedState(PlayerState state);
    }

    private abstract class BasePortReader : IPortReader
    {
        protected List<PlayerState> managedStates = new List<PlayerState>();
        public abstract bool IsPortOpen { get; }
        public abstract string PortName { get; }
        public abstract uint LastReceivedHeartRate { get; }
        public abstract bool IsHeartRateStable { get; }
        public abstract void Read();
        public abstract void Destroy();

        public void AddManagedState(PlayerState state) {
            if (!managedStates.Contains(state)) {
                managedStates.Add(state);
            }
        }

        public void RemoveManagedState(PlayerState state) => managedStates.Remove(state);
    }

    private class FakePortReader : BasePortReader
    {
        private uint _lastReceivedHeartRate = 0;
        public override bool IsPortOpen => Application.isPlaying;
        public override string PortName => "Fake port";
        public override uint LastReceivedHeartRate => _lastReceivedHeartRate;
        public override bool IsHeartRateStable => _unstableMask == 0;

        private int _unstableMask = 0;

        public FakePortReader() {
            Debug.Log(
                "Created fake port reader, use W/S/Space for first player, I/K/L for others and H to toggle stability.");
        }

        public override void Read() {
            var shouldToggle = Input.GetKeyDown(KeyCode.H);
            if (shouldToggle) {
                _unstableMask = (_unstableMask + 1) % 4;
                print($"Fake-port-reader: Changed stability [{(_unstableMask & 1)}, {(_unstableMask & 2)>>1}]");
            }
            for (var i = 0; i < managedStates.Count; ++i) {
                if ((_unstableMask & (1 << i)) == 0) {
                    var increase = Input.GetKeyDown(i == 0 ? KeyCode.W : KeyCode.I);
                    var decrease = Input.GetKeyDown(i == 0 ? KeyCode.S : KeyCode.K);
                    var change = 5U * (increase ? 1U : 0U) - (decrease ? 1U : 0U);
                    managedStates[i].Rate += change;
                    managedStates[i].BreakButton = Input.GetKey(i == 0 ? KeyCode.Space : KeyCode.L);
                }
            }
            if (managedStates.Count > 0)
                _lastReceivedHeartRate = managedStates[0].Rate;
        }

        public override void Destroy() {
        }
    }

    private class SerialPortReader : BasePortReader
    {
        private SerialPort port;

        public SerialPort Port => port;

        // buffer data as they arrive, until a new line is received
        private string bufferIn = "";

        private uint _lastReceivedHeartRate;
        private uint _lastAcceptedHeartRate;
        private float _lastAcceptedHeartRateTime;
        private ushort _stableReceptions;

        public override bool IsPortOpen => Port.IsOpen;
        public override string PortName => Port.PortName;
        public override uint LastReceivedHeartRate => _lastReceivedHeartRate;
        public override bool IsHeartRateStable => _stableReceptions == StableInputsToConsiderStableAgain;

        public SerialPortReader(string portName, int baudRate) {
            try {
                print($"Opening serial port: {portName}");
                port = new SerialPort(portName, baudRate);

                port.Open();

                // print ("default ReadTimeout: " + port.ReadTimeout);
                //port.ReadTimeout = 10;
                port.DiscardInBuffer();

                _stableReceptions = StableInputsToConsiderStableAgain;
                _lastAcceptedHeartRateTime = Time.unscaledTime;
            } catch (Exception ex) {
                Debug.LogError($"Couldn't open port {portName}: ");
                Debug.LogException(ex);
            }
        }

        public override void Read() {
            if (port == null || !port.IsOpen) return;
            while (port.BytesToRead > 0) {
                // BytesToRead crashes on Windows -> use ReadLine in a Thread
                string serialIn = port.ReadExisting();
                // prepend pending buffer to received data and split by line
                string[] lines = (bufferIn + serialIn).Split('\n');
                GetLastDataFromLines(lines, out var heartRate, out var breakButton);
                UpdateManagedStates(ControlHeartRateStability(heartRate), breakButton);
            }
        }

        private uint ControlHeartRateStability(uint heartRate) {
            if (heartRate == 0) return heartRate; // not valid anyways

            // not similar to last RECEIVED makes it unstable
            var stable = Diff(_lastReceivedHeartRate, heartRate) < 100;
            _lastReceivedHeartRate = heartRate;
            if (stable) {
                if (_stableReceptions < StableInputsToConsiderStableAgain) {
                    _stableReceptions += 1;
                    return 0;
                }

                if (WouldChangeBeTooFast(heartRate)) {
                    return 0;
                }

                _lastAcceptedHeartRate = heartRate;
                _lastAcceptedHeartRateTime = Time.unscaledTime;
                return heartRate;
            }

            _stableReceptions = 0;
            return 0;
        }

        private bool WouldChangeBeTooFast(uint heartRate) {
            var timeDiff = Time.unscaledTime - _lastAcceptedHeartRateTime;
            // heart rate change speed only makes sense if time diff is decent (otherwise is noisy)
            if (timeDiff < 0.02f) return false;
            var heartRateChangeSpeed = Diff(_lastAcceptedHeartRate, heartRate)
                                       / (Time.unscaledTime - _lastAcceptedHeartRateTime);
            return heartRateChangeSpeed > MaxHeartRateChangePerSecond;
        }

        private static uint Diff(uint a, uint b) {
            if (b > a) return b - a;
            return a - b;
        }

        private void GetLastDataFromLines(string[] lines, out uint heartRate, out short breakButton) {
            heartRate = 0;
            breakButton = -1;

            // If last line is not empty, it means the line is not complete (new line did not arrive yet), 
            // We keep it in buffer for next data.
            var nLines = lines.Length;
            bufferIn = lines[nLines - 1];

            if (nLines <= 1)
                return;

            for (var lineIndex = nLines - 2; lineIndex >= 0; lineIndex--) {
                var line = lines[lineIndex];
                var data = line.Split(';');

                if (data.Length != 2)
                    continue; // malformed line(?)

                try {
                    if (heartRate == 0)
                        heartRate = uint.Parse(data[0]);
                    if (breakButton < 0)
                        breakButton = short.Parse(data[1]);

                    if (heartRate > 0 && breakButton >= 0)
                        break; // Both already set, we are done!
                } catch {
                    Debug.LogWarning($"Malformed line? '{line}'");
                }
            }
        }

        private void UpdateManagedStates(uint heartRate, short isBreaking) {
            if (heartRate == 0 && isBreaking < 0)
                return; // Nothing to update
            foreach (var state in managedStates) {
                if (isBreaking >= 0)
                    state.BreakButton = isBreaking == 1;

                if (heartRate <= 0)
                    continue; // No heart-rate to set

                if (state.Rate == 0 || !state.IsHeartRateConnected) {
                    state.Rate = heartRate;
                } else {
                    state.Rate = (uint) Mathf.RoundToInt(
                        0.8f * state.Rate + 0.2f * heartRate
                    );
                }
            }
        }

        public override void Destroy() {
            if (port.IsOpen) {
                print($"Closing {port.PortName}");
                port.Close();
            }

            port = null;
        }
    }

    #region Static vars

    // Singleton instance
    private static PortReading s_instance;

    // Players
    private static PlayerState[] s_playerStates = { };
    private static IPortReader[] s_portReaders = null;

    #endregion

    #region Static props

    private static bool ExistsPlayer(int player) => player > 0 && player <= s_playerStates.Length;
    private static PlayerState GetPlayerState(int player) => ExistsPlayer(player) ? s_playerStates[player - 1] : null;

    public static bool IsHRConnected(int player) => ExistsPlayer(player) && GetPlayerState(player).IsHeartRateConnected;
    public static bool IsBreaking(int player) => ExistsPlayer(player) && GetPlayerState(player).BreakButton;
    public static uint HeartRate(int player) => ExistsPlayer(player) ? GetPlayerState(player).Rate : 0;
    public static uint AverageHeartRate(int player) => ExistsPlayer(player) ? GetPlayerState(player).AverageRate : 0;
    public static uint MaxHeartRate(int player) => ExistsPlayer(player) ? GetPlayerState(player).MaxHeartRate : 0;
    public static int PlayersCount => s_playerStates?.Length ?? 0;

    public static string Port(int idx) =>
        s_portReaders != null && idx >= 0 && idx < s_portReaders.Length ? s_portReaders[idx].PortName : "no reader";

    public static bool IsPortOpen(int idx) =>
        s_portReaders != null && idx >= 0 && idx < s_portReaders.Length && s_portReaders[idx].IsPortOpen;

    /// <summary>
    /// Just for debug, do not use to control anything!!!
    /// </summary>
    public static uint PortLastHeartRate(int idx) =>
        s_portReaders != null && idx >= 0 && idx < s_portReaders.Length ? s_portReaders[idx].LastReceivedHeartRate : 0;

    public static bool IsPortHRStable(int idx) =>
        s_portReaders != null && idx >= 0 && idx < s_portReaders.Length && s_portReaders[idx].IsHeartRateStable;

    public static int PortsCount => s_portReaders?.Length ?? 0;

    #endregion

    public string[] ports;
    public int baudRate = 115200;

    public static void RecordPlayerStats() {
        foreach (var playerState in s_playerStates) {
            playerState.RecordStats();
        }
    }

    public static void StopRecordingPlayerStats(int player) {
        if (ExistsPlayer(player)) {
            GetPlayerState(player).StopRecordingStats();
        }
    }

    // On awake, make sure there is only one
    private void Awake() {
        if (gameObject.GetComponents<Component>().Length > 2) {
            Debug.LogError("PortReading must not share gameObject with other components!");
            Destroy(gameObject);
            return;
        }

        if (s_instance == null) {
            s_instance = this;
            DontDestroyOnLoad(gameObject);
            RecreatePlayersAndPorts();
        } else {
            Debug.Log("You can't have more than one instance of PortReading in your game!" +
                      "So we destroy this one (it is fine if you just changed from other scene with PortReading)");
            Destroy(gameObject);
        }
    }

    public void RecreatePlayersAndPorts() {
        s_playerStates = new PlayerState[ports.Length];
        for (var i = 0; i < ports.Length; i++) {
            s_playerStates[i] = new PlayerState();
        }

        if (s_portReaders != null) {
            foreach (var portReader in s_portReaders) {
                portReader.Destroy();
            }
        }

        // Create port readers and assigned managed player states
        var dictionary = new Dictionary<string, IPortReader>();
        for (var i = 0; i < ports.Length; i++) {
            var portName = ports[i];
            IPortReader portReader;
            if (dictionary.ContainsKey(portName)) {
                portReader = dictionary[portName];
            } else {
                if (portName == FakePortName) {
                    portReader = new FakePortReader();
                } else {
                    portReader = new SerialPortReader(portName, baudRate);
                }

                dictionary.Add(portName, portReader);
            }

            portReader.AddManagedState(s_playerStates[i]);
        }

        s_portReaders = new IPortReader[dictionary.Count];
        var j = 0;
        foreach (var keyValue in dictionary) {
            s_portReaders[j++] = keyValue.Value;
        }
    }

    public void OnDestroy() {
        if (s_instance != this) return;
        s_playerStates = null;
        if (s_portReaders != null) {
            foreach (var portReader in s_portReaders) {
                portReader.Destroy();
            }

            s_portReaders = null;
        }

        s_instance = null;
    }

    private void Update() {
        if (_coroutinesRunning == 0) {
            StartCoroutine(ReadSerialLoop());
        } else {
            if (_coroutinesRunning > 1) {
                Debug.LogWarning($"Too many coroutines running! {_coroutinesRunning} of them.");
            }

            _coroutinesRunning = 0; // Reset, the coroutines will increase it themselves
        }
    }

    private IEnumerator ReadSerialLoop() {
        while (true) {
            if (!enabled) {
                yield return new WaitUntil(() => enabled);
            }

            _coroutinesRunning++;

            if (s_portReaders == null) {
                yield break;
            }

            foreach (var portReader in s_portReaders) {
                try {
                    portReader.Read();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            yield return null;
        }
    }
}