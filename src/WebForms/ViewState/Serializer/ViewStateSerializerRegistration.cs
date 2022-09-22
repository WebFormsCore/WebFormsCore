namespace System.Web.Serializer;

public record ViewStateSerializerRegistration(byte Id, Type Type, Type SerializerType);