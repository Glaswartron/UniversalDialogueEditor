using System;

public interface IConditional
{
    public void SetCondition(UDSCondition condition);
    public UDSCondition? GetCondition();
}
