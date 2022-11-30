using System.Reflection;

namespace GiftyQueryLib.Translators.Models
{
    /// <summary>
    /// Simplified member data
    /// </summary>
    public class MemberData
    {
        /// <summary>
        /// Property or Field type
        /// </summary>
        public Type? MemberType { get; init; }

        /// <summary>
        /// Type of Caller
        /// </summary>
        public Type? CallerType { get; init; }

        /// <summary>
        /// Info about Field or Property
        /// </summary>
        public MemberInfo? MemberInfo { get; init; }

        /// <summary>
        /// Method name (only if Member is Method)
        /// </summary>
        public string? MethodName { get; init; }

        /// <summary>
        /// Left Side Member Info
        /// </summary>
        public MemberInfo? LeftSideMemberInfo { get; init; }
    }
}
