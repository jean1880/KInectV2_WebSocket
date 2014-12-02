using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fleck;
using Microsoft.Kinect;

namespace Kinect.Server
{
    /**
     * Main program module, is run on program initialization
     * @class Program
     */
    class Program
    {
        static int port = 1337; // specifies which port to run the server from
        static string serverAddress = "ws://127.0.0.1:";
        static List<IWebSocketConnection> _clients = new List<IWebSocketConnection>();

        static Body[] _skeletons = new Body[6];

        static Mode _mode = Mode.Color;

        static CoordinateMapper _coordinateMapper;

        static MultiSourceFrameReader _reader;

        /**
         * Static main
         * @method Main
         * @param {String[]} args
         * @static
         */
        static void Main(string[] args)
        {
            InitializeConnection();
            InitilizeKinect();

            Console.ReadLine();
        }

        /**
         * Initializees the websocket server
         * @method InitializeConnection
         * @private
         */
        private static void InitializeConnection()
        {
            var server = new WebSocketServer(serverAddress + port);

            // starts the server, and begins listening to the socket
            server.Start(socket =>
            {
                //when server is initiallized,add the socket to listen to
                socket.OnOpen = () =>
                {
                    _clients.Add(socket);
                };

                // when the server is done, ensure the socket is closed
                socket.OnClose = () =>
                {
                    _clients.Remove(socket);
                };

                // on an incoming message, check to see which frame data is being requested
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

                    // write a response line to the console window, confirming message, and switch
                    Console.WriteLine("Switched to " + message);
                };
            });
        }

        /**
         * Initializes the Microsoft Kinect sensor, and sets which framme sources to monitor from the sensor
         * @method InitilizeKinect
         * @private
         */
        private static void InitilizeKinect()
        {
            /*
             * get the default sensor, in this case there is only one sensor, so the function will just return the first one
             * if more thanone sensor is  required, then you would have to specify which sensor through
             * */
            KinectSensor sensor = KinectSensor.GetDefault();

            if (sensor != null)
            {
                // set which framme sources to listen to
                _reader = sensor.OpenMultiSourceFrameReader(
                    FrameSourceTypes.Color | 
                    FrameSourceTypes.Depth | 
                    FrameSourceTypes.Infrared | 
                    FrameSourceTypes.Body 
                );
                _reader.MultiSourceFrameArrived += _reader_MultiSourceFrameArrived;

                _coordinateMapper = sensor.CoordinateMapper;

                // open the kinect sensor to begin listening
                sensor.Open();
            }
        }

        /**
         * Listener function, recieves the frames from the kinect sensor, and identifies the type of frame returned
         * @method _reader_MultiSourceFrameArrived
         * @param {Object} sender
         * @param {MultiSourceFrameArrivedEventArgs} e
         * @private
         */
        private static void _reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Get a reference to the multi-frame
            MultiSourceFrame reference = e.FrameReference.AcquireFrame();

            // if returned frame is a colourframe, return the colour frame blob
            using (ColorFrame frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        // serialize the data
                        var blob = frame.Serialize();

                        foreach (var socket in _clients)
                        {
                            socket.Send(blob);
                        }
                    }
                }
            }

            // if returned frame is a depthframe, return the depth frame blob
            using (DepthFrame frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Depth)
                    {
                        // serialize the data
                        var blob = frame.Serialize();

                        foreach (var socket in _clients)
                        {
                            socket.Send(blob);
                        }
                    }
                }
            }

            // If returned frame is a skeletal/body frame,populate the bodies in the frame to a list and return the result through the socket
            using (BodyFrame frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    
                    frame.GetAndRefreshBodyData(_skeletons);

                    // fetch the bodies monitored in the frame
                    var users = _skeletons.Where(s => s.IsTracked == true).ToList();

                    if (users.Count > 0)
                    {
                        // reialize the data to be returneed
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
