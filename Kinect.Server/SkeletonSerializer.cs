using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Kinect;
using System.Windows;

namespace Kinect.Server
{
    /**
     * Serializes a Kinect skeleton to JSON fromat.
     * @class SkeletonSerializer
     * @statiic
     */
    public static class SkeletonSerializer
    {
        [DataContract]
        class JSONSkeletonCollection
        {
            [DataMember(Name = "skeletons")]
            public List<JSONSkeleton> Skeletons { get; set; }
        }

        [DataContract]
        class JSONSkeleton
        {
            [DataMember(Name = "id")]
            public string ID { get; set; }

            [DataMember(Name = "joints")]
            public List<JSONJoint> Joints { get; set; }
        }

        [DataContract]
        class JSONJoint
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "x")]
            public double X { get; set; }

            [DataMember(Name = "y")]
            public double Y { get; set; }

            [DataMember(Name = "z")]
            public double Z { get; set; }
        }

        /**
         * Serializes an array of Kinect skeletons into an array of JSON skeletons.
         * @method Serialize
         * @param {List<Body>} skeletons
         * @param {CoordinateMapper} mapper
         * @param {Mode} mode
         * @return {String} A JSON representation of the skeletons
         */
        public static string Serialize(this List<Body> skeletons, CoordinateMapper mapper, Mode mode)
        {
            // instantiate the skeleton JSON object
            JSONSkeletonCollection jsonSkeletons = new JSONSkeletonCollection { Skeletons = new List<JSONSkeleton>() };


            // For each skeleton set in the camera view
            foreach (var skeleton in skeletons)
            {
                // set skeleton inside of JSON data
                JSONSkeleton jsonSkeleton = new JSONSkeleton
                {
                    ID = skeleton.TrackingId.ToString(),
                    Joints = new List<JSONJoint>()
                };

                // loop through each joint in the skeleton
                for (int i = 0; i < skeleton.Joints.Count; i++ )
                {
                    var joint = skeleton.Joints.ElementAt(i);
                    Point point = new Point();

                    // depending on which mode the user is in, system will map the points to the frame spacce 
                    switch (mode)
                    {
                        case Mode.Color:
                            ColorSpacePoint colorPoint = new ColorSpacePoint(); 
                            colorPoint = mapper.MapCameraPointToColorSpace(joint.Value.Position);
                            point.X = colorPoint.X;
                            point.Y = colorPoint.Y;
                            break;
                        case Mode.Depth:
                            DepthSpacePoint depthPoint = new DepthSpacePoint();
                            depthPoint = mapper.MapCameraPointToDepthSpace(joint.Value.Position);
                            point.X = depthPoint.X;
                            point.Y = depthPoint.Y;
                            break;
                        default:
                            break;
                    }

                    // add the joints to the json object
                    jsonSkeleton.Joints.Add(new JSONJoint
                    {
                        Name = joint.Key.ToString().ToLower(),
                        X = point.X,
                        Y = point.Y,
                        Z = joint.Value.Position.Z
                    });
                }

                // add the completed skeleton to the json object
                jsonSkeletons.Skeletons.Add(jsonSkeleton);
            }

            // return the json string
            return Serialize(jsonSkeletons);
        }

        /**
         * Serializes an object to JSON.
         * @method Serialize
         * @param {Object} obj
         * @return {string}
         * @static
         */
        private static string Serialize(object obj)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);

                return Encoding.Default.GetString(ms.ToArray());
            }
        }
    }
}
