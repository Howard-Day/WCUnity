using System;
using UnityEngine;

public enum Side
{
    Front, Back, Left, Right
}

public interface IReadOnlyArmorStatus {
	float Back { get; }
	float Front { get; }
	float Left { get; }
	float Right { get; }
	float Sum { get; }
}

[System.Serializable]
public class ArmorStatus : IReadOnlyArmorStatus {
	[SerializeField] private float front;
	[SerializeField] private float back;
	[SerializeField] private float left;
	[SerializeField] private float right;

	public ArmorStatus(float front, float back, float left, float right) {
		this.front = front;
		this.back = back;
		this.left = left;
		this.right = right;
	}

    public ArmorStatus(IReadOnlyArmorStatus source)
    {
        this.front = source.Front;
        this.back = source.Back;
        this.left = source.Left;
        this.right = source.Right;
    }

    public float Front { get => front; set => front = value; }
    public float Back { get => back; set => back = value; }
    public float Left { get => left; set => left = value; }
    public float Right { get => right; set => right = value; }

    public float Sum => front + back + left + right;

    public float GetValue(Side side)
    {
        if (side == Side.Front) return front;
        if (side == Side.Back) return back;
        if (side == Side.Left) return left;
        if (side == Side.Right) return right;
        throw new System.NotImplementedException("Unrecognized side " + side);
    }

    public void SetValue(Side side, float value)
    {
        if (side == Side.Front) front = value;
        else if (side == Side.Back) back = value;
        else if (side == Side.Left) left = value;
        else if (side == Side.Right) right = value;
        else throw new System.NotImplementedException("Unrecognized side " + side);
    }

    public ArmorStatus Clone()
    {
        return new ArmorStatus(front, back, left, right);
    }
}

public interface IReadOnlyShieldStatus {
	float Back { get; }
	float Front { get; }
	float Sum { get; }
}

[System.Serializable]
public class ShieldStatus : IReadOnlyShieldStatus {
	[SerializeField] private float front;
	[SerializeField] private float back;

	public ShieldStatus(float front, float back) {
		this.front = front;
		this.back = back;
	}

    public ShieldStatus(IReadOnlyShieldStatus source)
    {
        this.front = source.Front;
        this.back = source.Back;
    }

    public float Front { get => front; set => front = value; }
    public float Back { get => back; set => back = value; }

    public float Sum => front + back;

    public float GetValue(Side side)
    {
        if (side == Side.Front) return front;
        if (side == Side.Back) return back;
        throw new System.NotImplementedException("Unrecognized side " + side);
    }

    public void SetValue(Side side, float value)
    {
        if (side == Side.Front) front = value;
        else if (side == Side.Back) back = value;
        throw new System.NotImplementedException("Unrecognized side " + side);
    }

    public ShieldStatus Clone()
    {
        return new ShieldStatus(front, back);
    }
}
