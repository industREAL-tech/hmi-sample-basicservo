using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using industREAL.CAN.CANGW;
using industREAL.CAN.CORE;
using industREAL.HMI.CAN;
using industREAL.Utils;
using Avalonia.Media;
using System;

namespace industREAL.HMI.Sample.BasicServo
{
    public partial class MainWindow : Window
    {
        ServoDevice         _servoDeviceX;
        CanDispatcher       _canDispatcher;
        OnboardAsyncCAN     _canBus;
        JoystickManager     _joystickManager;

        static readonly IBrush GreenBrush = new SolidColorBrush(Color.Parse("#00B894"));
        static readonly IBrush RedBrush   = new SolidColorBrush(Color.Parse("#E74C3C"));

        public MainWindow()
        {
            InitializeComponent();

            API.BrightnessControl.SetBrightnessPercent(15);         
            // Load branding logo
            try
            {
                var logoPath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "industreal-logo-OK.png"));
                if (!System.IO.File.Exists(logoPath))
                    logoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Resources", "industreal-logo-OK.png");
                if (System.IO.File.Exists(logoPath))
                    BrandingLogo.Source = new Bitmap(logoPath);
            }
            catch { /* branding is optional */ }

            #region UI
            var uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            uiTimer.Tick += (_, _) =>
            {
                PositionGauge.Value = _servoDeviceX.TargetPosition;
                TargetText.Text = _servoDeviceX.TargetPosition.ToString();
                ActualGauge.Value = _servoDeviceX.ActualPosition;
                ActualText.Text = _servoDeviceX.ActualPosition.ToString();

                var diff = Math.Abs(_servoDeviceX.TargetPosition - _servoDeviceX.ActualPosition);
                ActualGauge.FillBrush = diff > 2400 ? RedBrush : GreenBrush;

                double torque = _servoDeviceX.TorqueLimit;
                TorqueText.Text = torque.ToString();
                TorqueBar.Width = Math.Clamp(torque / 1000.0, 0, 1) * 500.0;

                double voltage = _servoDeviceX.measuredVoltage;
                VoltageText.Text = $"{voltage:F1} V";
                VoltageBar.Width = Math.Clamp(voltage / 50.0, 0, 1) * 500.0;

                double temp = _servoDeviceX.measuredTemperaturePcb;
                TempText.Text = $"{temp:F1} °C";
                TempBar.Width = Math.Clamp((temp + 20.0) / 100.0, 0, 1) * 500.0;

                CurrentText.Text = $"Current: {_servoDeviceX.measuredCurrent} mA";
                LastSeenText.Text = $"Last seen: {_servoDeviceX.lastSeen:HH:mm:ss.fff}";
            };
            uiTimer.Start();
            #endregion  UI

            //_servoDeviceX = new ServoDevice(0x00020040, "X axis");
            _servoDeviceX = new ServoDevice(0x00020000, "X axis");
            _canBus = new();

            _canDispatcher = new(
                enqueue: f => _canBus.addToMultipleFrame((CanFrame)f.Clone()),
                flush: () => _canBus.FlushMultipleFrames());

            _canBus.FrameReceived += f => _canDispatcher.Route(f);

            _canDispatcher.RegisterMasked(_servoDeviceX.canBaseID, 0x1FFFFFF0u, _servoDeviceX.HandleMessage);
            
            _canDispatcher.RegisterPeriodicOutput(15, _servoDeviceX.getPeriodicOutput);
            
            _canBus.StartCanLoopAsync();

            /* Servo initial values */
            _servoDeviceX.TorqueLimit       = 72;
            _servoDeviceX.SpeedLimit        = 18000;
            _servoDeviceX.TargetPosition    = 0;
            _servoDeviceX.Activate();

            //API.WaitForDebugger();

            #region JOYSTICK
            _joystickManager = new JoystickManager();

            _joystickManager.OnState += (x, y, col) =>
            {
                int cyclicX = x;
                uint torq = (uint)((col + 32768) / 100);
                _servoDeviceX.TargetPosition = -1 * cyclicX;
                _servoDeviceX.TorqueLimit = (ushort)(60 + torq);
            };

            _joystickManager.Start();
            #endregion JOYSTICK


        }
    }
}