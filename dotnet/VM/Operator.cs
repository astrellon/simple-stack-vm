namespace SimpleStackVM
{
    public enum Operator : byte
    {
        Unknown,
        Push, Pop, Swap, Copy,
        Call, Return,
        Jump, JumpTrue, JumpFalse
    }
}