using PuppyDragon.uNody;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    public class RerouteReference
    {
        private NodePort port;
        private int connectionIndex;
        private int rerouteIndex;

        public NodePort Port => port;
        public int ConnectionIndex => ConnectionIndex;
        public int RerouteIndex => rerouteIndex;
        public Vector2 Value
        {
            get
            {
                var connection = port.GetConnection(connectionIndex);
                if (connection.Reroutes.Count <= rerouteIndex)
                    return Vector2.zero;
                else
                    return connection.Reroutes[rerouteIndex];
            }
        }

        public RerouteReference(NodePort port, int connectionIndex, int rerouteIndex)
        {
            this.port = port;
            this.connectionIndex = connectionIndex;
            this.rerouteIndex = rerouteIndex;
        }

        public void InsertPoint(Vector2 position)
            => port.GetConnection(connectionIndex).InsertReroute(rerouteIndex, position);
        public void SetPoint(Vector2 position)
            => port.GetConnection(connectionIndex).SetReroute(rerouteIndex, position);
        public void RemovePoint()
            => port.GetConnection(connectionIndex).RemoveReroute(rerouteIndex);
    }
}
