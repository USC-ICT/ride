using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ride.AI
{
    /// <summary>
    /// The update status of the node
    /// </summary>
    public enum BTNodeStatus
    {
        Success,
        Failure,
        Running,
    }

    /// <summary>
    /// A Node in a behaviour tree
    /// </summary>
    public interface IBTNode
    {
        [JsonIgnore]
        IBTNode parent { get; }

        /// <summary>
        /// The name of the node in order to identify it's type
        /// </summary>
        [JsonProperty(Order = 1)]
        string name { get; }

        /// <summary>
        /// Called before being updated if the node is node status is not already running
        /// </summary>
        void Init();

        /// <summary>
        /// Updates the node and returns its current status
        /// </summary>
        /// <returns></returns>
        BTNodeStatus Update(float dt);
    }

    /// <summary>
    /// A behaviour tree node that can have multiple children
    /// </summary>
    public interface IBTNodeComposite : IBTNode
    {
        /// <summary>
        /// The collection of children nodes
        /// </summary>
        [JsonProperty(Order = 2)]
        IEnumerable<IBTNode> children { get; }

        /// <summary>
        /// Adds a child node to this node
        /// </summary>
        /// <param name="child"></param>
        void AddChild(IBTNode child);

        /// <summary>
        /// Returns the child node at the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        IBTNode GetChild(int index);

        /// <summary>
        /// The number of children this node has
        /// </summary>
        int numChildren { get; }
    }

    /// <summary>
    /// A behaviour tree node that has only 1 child
    /// </summary>
    public interface IBTNodeDecorator : IBTNode
    {
        /// <summary>
        /// The child node
        /// </summary>
        [JsonProperty(Order = 2)]
        IBTNode child { get; }

        /// <summary>
        /// Set the one and only child of this node
        /// </summary>
        /// <param name="child"></param>
        void SetChild(IBTNode child);
    }

    public interface IBTLeafNode : IBTNode
    {
        void Setup(RideID agent, Dictionary<string, object> treeVars);

        /// <summary>
        /// Setup functions for the leaf nodes instantiated by the authoring tool.
        /// </summary>
        /// <param name="guidID">guidID of the node UI created by the authoring tool. Used for identifying each leaf nodes</param>
        void Setup(RideID agent, Dictionary<string, object> treeVars, string guidID);

        /// <summary>
        /// Returns true if the variable with the given name exists in the tree
        /// </summary>
        /// <param name="varName"></param>
        /// <returns>True if the variable exists</returns>
        bool DoesVarExist(string varName);

        /// <summary>
        /// Set the value of the variable
        /// </summary>
        /// <typeparam name="T">the type of variable</typeparam>
        /// <param name="varName">the name of the variable</param>
        /// <param name="value">the value of the variable</param>
        void SetVar<T>(string varName, T value);

        /// <summary>
        /// Returns the value of the tree variable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="varName">the tree variable name</param>
        /// <returns>The value of the varaible. default of T if the var doesn't exist</returns>
        T GetVar<T>(string varName);
    }
}
