using CommunityToolkit.Mvvm.Messaging;

namespace MrCapitalQ.AutoUnlaunch.Tests;

/// <summary>
/// <see cref="IMessenger"/> requires a token object to be passed some calls but the default implementation is not
/// accessible. So, this class is used instead when checking for received calls or configuring substitute behaviors
/// to represent a token of any type.
/// </summary>
internal class TestMessengerToken : Arg.AnyType, IEquatable<TestMessengerToken>
{
    public bool Equals(TestMessengerToken? other) => throw new NotImplementedException();
}
