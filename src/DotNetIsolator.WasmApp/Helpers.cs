﻿using MessagePack;
using MessagePack.Resolvers;
using System.Reflection;

namespace DotNetIsolator.WasmApp;

public static class Helpers
{
    public static unsafe object? Deserialize<T>(byte* value, int valueLength)
    {
        // Instead of using ToArray (or UnmanagedMemoryStream) you could have a pool of byte[]
        // buffers on the guest side and have the host serialize directly into their memory, then
        // there would be no allocations on either side, and this code could work with a Memory<byte>
        // for whatever region within one of those buffers.
        var memoryCopy = new Span<byte>(value, valueLength).ToArray();
        return Deserialize<T>(memoryCopy);
    }

    internal static unsafe object? Deserialize<T>(Memory<byte> value)
    {
        var result = MessagePackSerializer.Deserialize<T>(value, MessagePackSerializer.Typeless.DefaultOptions);

        // Console.WriteLine($"Deserialized value of type {result?.GetType().FullName} with value {result}");
        return result;
    }

    public static byte[] Serialize(object value)
    {
        // TODO: Should we really be pinning the result value here, or is it safe to return
        // a MonoObject* to unmanaged code and then use mono_gchandle_new(..., true) from there?
        return MessagePackSerializer.Serialize(value, ContractlessStandardResolverAllowPrivate.Options);
    }

    public static void GetMemberHandle(object member, ref MemberTypes memberType, ref IntPtr handle)
    {
        memberType = (member as MemberInfo)?.MemberType ?? 0;
        switch (member)
        {
            case Type type:
                handle = type.TypeHandle.Value;
                break;
            case MethodBase method:
                handle = method.MethodHandle.Value;
                break;
            case FieldInfo field:
                handle = field.FieldHandle.Value;
                break;
            default:
                handle = IntPtr.Zero;
                break;
        }
    }
}