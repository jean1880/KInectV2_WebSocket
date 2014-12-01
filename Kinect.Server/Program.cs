using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fleck;
using Microsoft.Kinect;

namespace Kinect.Server
{
    class Program
    {
        static int port = 1337; // specifies which port to run the server from
        static List<IWebSocketConnection> _clients = new List<IWebSocketConnection>();

        static Body[] _skeletons = new Body[6];

        static Mode _mode = Mode.Color;

        static CoordinateMapper _coordinateMapper;

        static MultiSourceFrameReader _reader;

        static void Main(string[] args)
        {
            InitializeConnection();
            InitilizeKinect();

            Console.ReadLine();
        }

        private static void InitializeConnection()
        {
            var server = new WebSocketServer("http://0.0.0.0:" + port);

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    _clients.Add(socket);
                };

                socket.OnClose = () =>
                {
                    _clients.Remove(socket);
                };

                socket.OnMessage = message =>
                {
                    switch (message)
                    {
                        case "Color":
                            _mode = Mode.Color;
                            break;
                        case "Depth":
                            _mode = Mode.Depth;
                            break;
                        default:
                            break;
                    }

                    Console.WriteLine("Switched to " + message);
                };
            });
        }

        private static void InitilizeKinect()
        {
            KinectSensor sensor = KinectSensor.GetDefault();

            if (sensor != null)
            {
                _reader = sensor.OpenMultiSourceFrameReader(
                    FrameSourceTypes.Color | 
                    FrameSourceTypes.Depth | 
                    FrameSourceTypes.Infrared | 
                    FrameSourceTypes.Body 
                );
                _reader.MultiSourceFrameArrived += _reader_MultiSourceFrameArrived;

                _coordinateMapper = sensor.CoordinateMapper;

                sensor.Open();
            }
        }

        private static void _reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Get a reference to the multi-frame
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        var blob = frame.Serialize();

                        foreach (var socket in _clients)
                        {
                            socket.Send(blob);
                        }
                    }
                }
            }

            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        var blob = frame.Serialize();

                        foreach (var socket in _clients)
                        {
                            socket.Send(blob);
                        }
                    }
                }
            }

            using (BodyFrame frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    
                    frame.GetAndRefreshBodyData(_skeletons);

                    var users = _skeletons.Where(s => s.IsTracked == true).ToList();

                    if (users.Count > 0)
                    {
                        string json = users.Serialize(_coordinateMapper, _mode);

                        foreach (var socket in _clients)
                        {
                            socket.Send(json);
                        }
                    }
                }
            }
        }

    }
}
