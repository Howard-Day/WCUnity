/// ©2018 - 2021 Kevin Foley.
/// See accompanying license file.

using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Various UnityEvent classes for commonly used data types and structures
/// </summary>
namespace OneManEscapePlan.Common.Scripts.Events {
	//C# types
	[Serializable]
	public class IntEvent : UnityEvent<int> { }
	[Serializable]
	public class UintEvent : UnityEvent<uint> { }
	[Serializable]
	public class ShortEvent : UnityEvent<short> { }
	[Serializable]
	public class LongEvent : UnityEvent<long> { }
	[Serializable]
	public class FloatEvent : UnityEvent<float> { }
	[Serializable]
	public class DoubleEvent : UnityEvent<double> { }
	[Serializable]
	public class ByteEvent : UnityEvent<byte> { }
	[Serializable]
	public class StringEvent : UnityEvent<string> { }
	[Serializable]
	public class BoolEvent : UnityEvent<bool> { }

	//Unity types
	[Serializable]
	public class GameObjectEvent : UnityEvent<GameObject> { }
	[Serializable]
	public class TransformEvent : UnityEvent<Transform> { }
	[Serializable]
	public class RigidbodyEvent : UnityEvent<Transform> { }
	[Serializable]
	public class ColliderEvent : UnityEvent<Collider> { }
	[Serializable]
	public class Collider2DEvent : UnityEvent<Collider2D> { }
	[Serializable]
	public class Vector2Event : UnityEvent<Vector2> { }
	[Serializable]
	public class Vector2IntEvent : UnityEvent<Vector2Int> { }
	[Serializable]
	public class Vector3Event : UnityEvent<Vector3> { }
	[Serializable]
	public class Vector3IntEvent : UnityEvent<Vector3Int> { }
	[Serializable]
	public class QuaternionEvent : UnityEvent<Quaternion> { }
	[Serializable]
	public class RectEvent : UnityEvent<Rect> { }
	[Serializable]
	public class BoundsEvent : UnityEvent<Bounds> { }
	[Serializable]
	public class SpriteEvent : UnityEvent<Sprite> { }
	[Serializable]
	public class MeshEvent : UnityEvent<Mesh> { }
	[Serializable]
	public class MaterialEvent : UnityEvent<Material> { }
	[Serializable]
	public class ColorEvent : UnityEvent<Color> { }
}
