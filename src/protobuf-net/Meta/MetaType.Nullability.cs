using System;
using System.Collections;
using System.Collections.Generic;

namespace ProtoBuf.Meta
{
    public sealed partial class MetaType
    {
        private class ExtraLayerValueMembers : IEnumerable<NullWrappedValueMemberData>
        {
            private readonly Dictionary<string, Type> _schemaMemberTypeMap = new();
            private readonly Dictionary<string, NullWrappedValueMemberData> _wrappedSchemaMembers = new();

            /// <returns>true, if doese not contain any <see cref="NullWrappedValueMemberData"/></returns>
            public bool IsEmpty() => _wrappedSchemaMembers.Count == 0;

            /// <summary>
            /// Processes another <see cref="ValueMember"/> of model 
            /// and adds a metadata used for it's representation
            /// </summary>
            public NullWrappedValueMemberData Add(string schemaTypeName, ValueMember valueMember)
            {
                if (!_schemaMemberTypeMap.ContainsKey(schemaTypeName))
                {
                    // no 'schemaTypeName' naming collision
                    var wrappedValueMember = new NullWrappedValueMemberData(valueMember, schemaTypeName);
                    _schemaMemberTypeMap[schemaTypeName] = wrappedValueMember.ItemType;
                    _wrappedSchemaMembers[wrappedValueMember.WrappedSchemaTypeName] = wrappedValueMember;
                    return wrappedValueMember;
                }

                var existingMemberType = _schemaMemberTypeMap[schemaTypeName];
                if (existingMemberType == valueMember.ItemType)
                {
                    // types of members are the same, we are fine using same schemaTypeName
                    var wrappedValueMember = new NullWrappedValueMemberData(valueMember, schemaTypeName);
                    _wrappedSchemaMembers[wrappedValueMember.WrappedSchemaTypeName] = wrappedValueMember;
                    return wrappedValueMember;
                }

                if (string.IsNullOrEmpty(valueMember.Name) || valueMember.Member?.Name == valueMember.Name)
                {
                    // there is no alternative name specified, so it's a 'schemaTypeName' collision
                    var wrappedValueMember = new NullWrappedValueMemberData(valueMember, schemaTypeName, hasSchemaTypeNameCollision: true);
                    _wrappedSchemaMembers[wrappedValueMember.WrappedSchemaTypeName] = wrappedValueMember;
                    return wrappedValueMember;
                }

                var alternativeSchemaTypeName = valueMember.Name;
                if (_schemaMemberTypeMap.ContainsKey(alternativeSchemaTypeName))
                {
                    // its an 'alternativeName' collision.
                    var wrappedValueMember = new NullWrappedValueMemberData(
                        valueMember, 
                        schemaTypeName, 
                        alternativeSchemaTypeName, 
                        hasSchemaTypeNameCollision: true);

                    _wrappedSchemaMembers[wrappedValueMember.WrappedSchemaTypeName] = wrappedValueMember;
                    return wrappedValueMember;
                }
                else
                {
                    // there is no 'alternativeName' collision
                    var wrappedValueMember = new NullWrappedValueMemberData(valueMember, schemaTypeName, alternativeSchemaTypeName);
                    _schemaMemberTypeMap[alternativeSchemaTypeName] = wrappedValueMember.ItemType;
                    _wrappedSchemaMembers[wrappedValueMember.WrappedSchemaTypeName] = wrappedValueMember;
                    return wrappedValueMember;
                }
            }

            /// <summary>
            /// Iterates over build wrapped valueMembers with additional representation metadata
            /// </summary>
            /// <returns></returns>
            public IEnumerator<NullWrappedValueMemberData> GetEnumerator() => _wrappedSchemaMembers.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
