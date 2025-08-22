## uNody

uNody is a node editor built on xNode with enhanced custom editor compatibility and new features like logic graphs, sub-graphs, a Blackboard, and custom variables.

it can be used for all kinds of tasks, such as a formula calculator, skill tree, state machine, and dialogue system.

<p align="center">
<img width="800" height="503" alt="Image" src="https://github.com/user-attachments/assets/56ffb236-4df6-428e-b81c-88a32e81db5d" />
</p>

### Key features
- Inherits the core strengths of xNode.
- Design versatile logic with Logic Graphs.
- Optimize editor performance using Sub-graphs.
- Manage variables inside and outside the graph with the Blackboard.

## Setup

1. Select Window > Package Manager in Unity Editor.
2. Select “+” Button > Add package from git URL .
3. Enter the following URL, https://github.com/GP-PDG/uNody.git
<p align="center">
<img width="300" height="168" alt="Image" src="https://github.com/user-attachments/assets/927bbb8e-e0cf-4e35-bea2-a6699045ed2e" />
</p>

## Getting Started
<details>
<summary><b>Basic Guide</b></summary>

NodeGraph is a standard, all-purpose graph. It's ideal for tasks that require a tree-like structure (e.g., a skill tree), direct control over the execution flow (e.g., a dialogue system), or for building formulas that return an immediate result.

<p align="center">
<img width="895" height="276" alt="Image" src="https://github.com/user-attachments/assets/043415ac-473f-4b68-9eef-fdcc9c0bf769" />
</p>

NodeGraph Example:
```c#
using PuppyDragon.uNody;

[CreateAssetMenu]
public class TestGraph : NodeGraph
{
}
```
Tip: Quickly create a Node script by selecting Assets > Create > uNody > NodeGraph C# Script

Node Example:
```c#
using PuppyDragon.uNody;

public class TestNode : Node
{
    [SerializeField]
    private InputPort<int> input;
    [SerializeField]
    private OutputPort<int> output = new(x => (x as TestNode).Result);

    public int Result => input.Value * input.Value;

    /* static method version
     * private OutputPort<int> output = new(GetResult);
     * 
     * public static int GetResult(Node node)
     *   => (node as NewMonoBehaviourScript).Result;
    */
}
```
Tip: Quickly create a Node script by selecting Assets > Create > uNody > Node C# Script

You can create static ports on a node using the `InputPort<T>` and `OutputPort<T>` types, which allow connections to other nodes. For an `OutputPort`, you must register a lambda or a static callback function in its constructor to define the value it will return.

You can access the value of an `InputPort` or `OutputPort` using `.Value` (for a single connection) or `.Values` (for multiple connections). If a port is configured to accept connections from various data types, you can access the data as an `object` type using `.DynamicValue` or `.DynamicValues`.

</br>

<p align="center">
<img width="399" height="383" alt="Image" src="https://github.com/user-attachments/assets/8615a8d8-b259-4cf8-9422-597c250f9eac" />
</p>

```c#
    [SerializeField]
    private InputPort<int>[] input;
```

You can dynamically create ports for a collection by declaring a port field as an array or List<T>. The node will then automatically display an individual port for each element in the collection.

</br>

<h3>Reroute</h3>

<p align="center">
    <img src="https://github.com/user-attachments/assets/c45083be-4b9e-4b93-bd76-ed5672ab502a" alt="Image" />
</p>

You can right-click while dragging a connection line to create a Reroute node, allowing you to freely organize the path of the connection.

</br>

<h3>Blackboard</h3>
<p align="center">
  <img src="https://github.com/user-attachments/assets/5d7767c1-9343-4850-ab14-816f9ae1c8b9" alt="Image" />
</p>

You can control variables inside and outside the graph by creating a Blackboard asset from `Assets > Create > uNody > Blackboard` and linking it to your NodeGraph.

```c#
    [SerializeField]
    private NodeGraph nodeGraph;

    void Start()
    {
        // get/set global variable
        nodeGraph.Blackboard.SetGlobalValue("testInt", 10);
        var globalValue = nodeGraph.Blackboard.GetGlobalValue<int>("testInt");

        // get/set local variable
        nodeGraph.Blackboard.SetLocalValue(nodeGraph, "testInt", 10);
        var localValue = nodeGraph.Blackboard.GetLocalValue<int>(nodeGraph, "testInt");
    }
```

You can access and manipulate variables stored in the Blackboard directly from your C# code. The Blackboard distinguishes between two types of variables.

`GlobalValue` represents a global variable owned by the Blackboard itself. This means that its value is consistent and can be retrieved as the same value across all graphs that share the same Blackboard asset. It's perfect for data that needs to be universally accessible, like global game settings or player scores.

`LocalValue` provides variables that are tied to a specific graph instance. Each graph that uses the Blackboard can maintain its own independent values for these local variables. This is incredibly useful when you want to use the same graph logic for multiple entities (e.g., different NPCs) but have each entity manage its own unique state or data.

</br>

```c#
    var clone = nodeGraph.Copy();
    clone.Blackboard.SetLocalValue(clone, "testInt", 10);
    Destroy(clone);
```

While Local Values are instance-specific, be aware that if multiple objects share the same graph instance, they will also share its local values. To give an object a completely independent set of variables, you must first create a copy of the graph.

</br>

```c#
    public class BlackboardFloat : BlackboardDynamicVar<float> {}
```

Blackboard variables are implemented as ScriptableObjects. You can easily add new types by creating a class that inherits from BlackboardDynamicVar<T>.

#### Accessing the Blackboard with Nodes

<p align="center">
<img width="813" height="444" alt="Image" src="https://github.com/user-attachments/assets/79c6eb64-f6d9-42f5-aef8-b242c58e3e5e" />
</p>

You can directly get and set Blackboard values from within the graph editor using a dedicated set of nodes:

*   `GetGlobal[Type]` / `SetGlobal[Type]`
*   `GetLocal[Type]` / `SetLocal[Type]`

These nodes allow you to build logic that dynamically interacts with the data stored in your Blackboard.

#### Supporting Custom Blackboard Types

When you introduce a new custom variable type for your Blackboard, you must also create the corresponding node classes to make them available in the editor's "Create Node" menu.

This process is simple and only requires you to create new classes that inherit from the provided generic base classes. This step is necessary because each node is a `ScriptableObject` and therefore requires its own C# script file to be recognized by the editor.

For example, if you added a `bool` variable type to the Blackboard, you would need to create the following four classes in separate script files:

```c#
// --- Global Nodes for 'bool' ---
public class GetGlobalBoolNode : GetGlobalValueNode<bool> {}
public class SetGlobalBoolNode : SetGlobalValueNode<bool> {}

// --- Local Nodes for 'bool' ---
public class GetLocalBoolNode : GetLocalValueNode<bool> {}
public class SetLocalBoolNode : SetLocalValueNode<bool> {}
```

</br>

<h3>InPoint/OutPoint</h3>
<p align="center">
<img width="1001" height="335" alt="Image" src="https://github.com/user-attachments/assets/ae227623-5e78-456e-ad9f-91374ba56d42" />
</p>

```c#
    [SerializeField]
    private NodeGraph nodeGraph;

    void Start()
    {
        nodeGraph.SetInValue("inputValue", 10);
        // result is 30
        int result = nodeGraph.GetOutValue<int>("result");
        // int result = (int)nodeGraph.GetOutValue("result");
    }
```

You can pass data directly to the graph or retrieve data from it using InPoint and OutPoint nodes. This allows you to use the graph as if it were a method.

<p align="center">
  <img src="https://github.com/user-attachments/assets/256e28a5-3d71-4ec6-96ba-2d2ec4ac432a" alt="Image" />
</p>

For `InPoint` and `OutPoint` nodes, the node's name is used as a variable name. You can rename it by selecting the node and pressing `F2`, or by choosing `Rename` from the node menu.

</br>

<h3>Adding Custom Types for InPoint/OutPoint</h3>

```c#
public class InIntNode : InPointNode<int> { }
public class OutIntNode : OutPointNode<int> { }
```

Want to add more types for InPoint/OutPoint? You can do so easily by creating a new class that inherits from InPointNode and OutPointNode.

</br>

<h3>Sub-Graph</h3>

<p align="center">
    <img src="https://github.com/user-attachments/assets/0402474f-5ee0-48f9-9299-f0e1981349c3" alt="Image" />
</p>

You can group nodes together using Sub Graphs. This prevents editor performance degradation caused by drawing many nodes at once and helps you organize your nodes visually.
When you create InPoint/OutPoint nodes inside a Sub Graph, corresponding ports will appear on the Sub Graph's GUI, allowing you to connect to them.

</br>

<h3>Port Settings</h3>

```c#
[SerializeField]
[PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
private InputPort<int> values;
```
You can customize the behavior and appearance of a port by using the [PortSettings] attribute. This attribute allows you to control three key aspects: when to show the input field, how connections are handled, and what data types are allowed.

Here is a breakdown of the available options:

#### `ShowBackingValue`

This setting controls the visibility of the port's default input field in the editor.

*   `Never`: The input field is never displayed.
*   `Unconnected`: The input field is only visible when the port has no active connections.
*   `Always`: The input field is always visible, even when the port is connected.

#### `ConnectionType`

This defines how a port handles incoming connections.

*   `Multiple`: Allows the port to accept an unlimited number of connections.
*   `Override`: Restricts the port to a single connection. Making a new connection will automatically replace the existing one.

#### `TypeConstraint`

This enforces type-safety rules for connections between ports.

*   `None`: Disables type checking, allowing any type of port to be connected.
*   `Strict`: Only allows connections between ports of the exact same type.
*   `Inherited`: Allows a connection if the input port's type is a parent class or interface of the output port's type (e.g., connecting an `int` output to an `object` input).
*   `InheritedInverse`: Allows a connection if the output port's type is a parent of the input port's type (e.g., connecting an `object` output to an `int` input, which requires the object to be castable).
*   `InheritedAny`: Allows a connection if the inheritance relationship works in either direction.

#### `isHideLabel`

Option, Hides the name of the variable.

</br>

<h3>Node Class Attributes</h3>

```c#
// Sets the width of the node. You can use a predefined size from the NodeSize enum (e.g., Small, Medium, Large) or specify a custom integer value.
[NodeWidth(NodeSize.Small)]
// Controls how the node appears in the "Create Node" context menu.
// You can define its menu path and name, and specify whether it should always be visible regardless of context.
[CreateNodeMenu(true)]
// Specifies the header color of the node. You can either reference a type to use its corresponding color from the Preferences settings or provide a custom hex color string.
// If [NodeFooterTint] is not set, the footer color will match the header.
[NodeHeaderTint(typeof(int))]
// Path to the icon image to be included in the node's title.
[NodeIcon("Assets/int.png)]
public class SumIntNode : Node
```

</br>

<h3>Filtering Nodes for a Specific Graph</h3>

You can control which nodes appear in the "Create Node" context menu to ensure that a graph only contains relevant nodes.
This is achieved by creating a custom editor for your graph and overriding the GetNodeMenuName method.
This allows you to create specialized graphs that only expose a curated set of nodes, improving usability and preventing errors.

```c#
namespace Sample
{
    public class TestGraph : NodeGraph {...}
    public class TestNode : Node {...}
}
```
```c#
using System;
using PuppyDragon.uNodyEditor;

// Associate this editor with the TestGraph class
[CustomNodeGraphEditor(typeof(TestGraph))]
public class TestGraphEditor : NodeGraphEditor
{
    public override string GetNodeMenuName(Type type)
    {
        // Only show nodes if their namespace is "Sample"
        if (type.Namespace != "Sample")
        {
            // Returning null hides the node from the context menu
            return null;
        }
        else
        {
            // Otherwise, use the default menu name
            return base.GetNodeMenuName(type);
        }
    }
}
```

</br>

<h3>Preferences</h3>
<p align="center">
<img width="474" height="400" alt="Image" src="https://github.com/user-attachments/assets/caeae3be-8687-4cb5-9df7-0615b302b1ee" />
</p>

You can customize the visual appearance of the graph editor, such as port colors and other GUI elements, to fit your project's needs. There are two ways to manage these settings: globally for all graphs or on a per-graph basis.


<b>Global Preferences</b>

Global settings for the uNody editor can be found in Unity's main preferences window.
Navigate to Preferences > uNody.
Here, you can configure default GUI settings and define the port colors for different data types (e.g., int, string, float).
These settings will apply to all NodeGraph editors unless they are overridden by a graph-specific editor.

<b>Graph-Specific Preferences</b>
```c#
using PuppyDragon.uNodyEditor;

[CustomNodeGraphEditor(typeof(TestGraph), true)]
public class TestGraphEdior : NodeGraphEditor { }
```
For more fine-grained control, you can create custom preference settings for a specific type of graph. This is done by creating a new editor class that inherits from `NodeGraphEditor`.

To ensure that a dedicated settings panel for your graph appears in the preferences window, you must pass `true` as the second argument to the `[CustomNodeGraphEditor]` attribute.
By doing this, you can define unique visual styles or behaviors for your graph, allowing it to have its own set of port colors or GUI configurations that override the global settings.

</br>

</details>
<details>
<summary><b>LogicGraph Guide</b></summary>
    
> **✅ Heads Up!**</br>
> `LogicGraph` is a derived class of `NodeGraph`, so you must read the `Basic Guide` before reading the `LogicGraph Guide`.

Logic Graph is a graph well-suited for visual scripting, creating sequences that must be executed in a specific order, and building logic that needs to run at runtime.

<p align="center">
<img width="1352" height="385" alt="Image" src="https://github.com/user-attachments/assets/2c340685-5f7d-42f4-9ad5-b54a106be258" />
</p>

NodeGraph Example:
```c#
using PuppyDragon.uNody.Logic;

[CreateAssetMenu]
public class TestGraph : LogicGraph
{
}
```
Tip: Quickly create a Node script by selecting Assets > Create > uNody > LogicGraph C# Script

LogicNode Example:
```c#
using PuppyDragon.uNody.Logic;

public class TestNode : LogicNode
{
    [SerializeField]
    private InputPort<int> input;
    [SerializeField]
    private OutputPort<int> output = new(x => (x as TestNode).Result);

    public int Result => input.Value * input.Value;

    /* static method version
     * private OutputPort<int> output = new(GetResult);
     * 
     * public static int GetResult(Node node)
     *   => (node as NewMonoBehaviourScript).Result;
    */
}
```
Tip: Quickly create a Node script by selecting Assets > Create > uNody > LogicNode C# Script

When you create a `LogicGraph`, it automatically generates `EntryPoint` and `ExitPoint` nodes. These serve as the entry and exit points for the graph's execution flow, respectively.
By default, every `LogicNode` includes an input flow port (`prevs`) and an output flow port (`next`). The `[ArrowPort]` attribute is used to display these ports as triangles, visually representing the direction of the logic.

#### Executing Logic from Code

You have full control over the execution of a `LogicGraph` directly from your scripts. This allows you to start, step through, and manage the graph's runtime state.

```c#
using UnityEngine;
using PuppyDragon.uNody.Logic;

public class TestScript : MonoBehaviour
{
    [SerializeField]
    private LogicGraph nodeGraph;

    void Start()
    {
        // Option 1: Execute the entire logic sequence at once.
        nodeGraph.Execute();
        
        // Option 2: Execute the logic one node at a time.
        nodeGraph.Step();

        // Check if the logic has finished. CurrentNode will be null at the end.
        if (nodeGraph.CurrentNode == null)
        {
            Debug.Log("Graph execution complete.");
        }

        /* 
         * --- Ensuring Graph Independence ---
         * If you need a unique runtime instance of the graph,
         * create a copy before executing it.
         *
         * var copyGraph = nodeGraph.Copy() as LogicGraph;
         * copyGraph.Execute();
         * Destroy(copyGraph);
         */
    }
}
```

#### Execution Methods

*   **`Execute()`**
    This method runs the entire logic sequence from the `EntryPoint` to an `ExitPoint` in a single call. It's the most straightforward way to run a self-contained piece of logic.

*   **`Step()`**
    This method executes only the *next* node in the sequence and then pauses execution. It's perfect for scenarios where you need fine-grained control, such as turn-based games, step-by-step debugging, or waiting for animations or user input between nodes.

#### Important: Graph Independence

As with other graph types, remember that the execution state (like the `CurrentNode` property) is tied to the graph **instance**.
If multiple objects need to run the same logic independently, you **must** create a separate copy for each one using `nodeGraph.Copy()`. This ensures that each instance has its own state and doesn't interfere with others.

</br>

<h3>ILogicConnector</h3>

```c#
namespace PuppyDragon.uNody.Logic
{
    public interface ILogicConnector
    {
    }
}
```

ILogicConnector signifies that a node's role is not to be the primary executor of logic, but rather to connect to other nodes.

<p align="center">
<img width="1070" height="384" alt="Image" src="https://github.com/user-attachments/assets/a38c2bf6-5119-474b-8f93-60f0c520432b" />
</p>

For example, the IfNode inherits ILogicConnector. Depending on its condition, it will return the node connected to the True port if the result is true, or the node connected to the False port if the result is false, effectively deciding the next node in the execution chain.

#### Important: Correctly Handling `ILogicConnector` Nodes

This is a crucial concept to understand when creating your own flow control nodes (like custom `if`, `while`, or `switch` statements).

An `ILogicConnector`'s job is to redirect flow, not to execute a final action. Therefore, if your output flow port could be connected to another `ILogicConnector` (e.g., the `body` of a `while` loop connecting directly to an `IfNode`), you cannot simply execute it directly. You must **traverse the chain of connectors** until you find the first node that is *not* an `ILogicConnector`. This is the actual node that should be executed next.

The correct way to handle this is by using a loop to resolve the true next node, as shown in the following example:

```c#
[ArrowPort, SerializeField]
[PortSettings(ShowBackingValue.Always, ConnectionType.Override, TypeConstraint.Strict)]
private OutputPort<ILogicNode> body = new(x => (x as WhileNode).body.Connection?.Node as ILogicNode);

public override void Execute()
{
    // ... your node's logic ...

    var next = body.Value;

    // This loop is essential. It resolves the actual next node to execute.
    // If the connected node is an ILogicConnector, it continues down the chain.
    while (next != null && typeof(ILogicConnector).IsAssignableFrom(next.GetType()))
    {
        next = next.Next;
    }

    // Now 'next' holds the first non-connector node, which can be safely executed.
    if (next != null)
    {
        next.Execute();
    }
    
    // ... rest of your logic ...
}
```

For a more detailed understanding of ILogicConnector and how to implement additional flow ports, please refer to the IfNode and WhileNode classes.

</details>
