// [Ready Design Corps] - [Mahjong Core] - Copyright 2018

namespace MahjongCore.Common.Attributes
{
    // From http://joelforman.blogspot.com/2007/12/enums-and-custom-attributes.html
    public interface IAttribute<T>
    {
        T Value { get; }
    }
}
