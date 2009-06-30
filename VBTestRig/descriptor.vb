
' Generated from 
' Generated from: ProtoGen/person.proto
Namespace people
    <Global.System.Serializable(), Global.ProtoBuf.ProtoContract(Name:="person")> _
    Partial Public Class person
        Implements Global.ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private _name As String
        <Global.ProtoBuf.ProtoMember(1, IsRequired:=True, Name:="name", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
        Public Property name() As String
            Get
                Return _name
            End Get

            Set(ByVal value As String)
                _name = value

            End Set
        End Property

        Private _id As Integer = 0
        <Global.ProtoBuf.ProtoMember(2, IsRequired:=False, Name:="id", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
        <Global.System.ComponentModel.DefaultValue(CType(0, Integer))> _
        Public Property id() As Integer
            Get
                Return _id
            End Get

            Set(ByVal value As Integer)
                _id = value

            End Set
        End Property

        Private _email As String = ""
        <Global.ProtoBuf.ProtoMember(3, IsRequired:=False, Name:="email", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
        <Global.System.ComponentModel.DefaultValue(CType("", String))> _
        Public Property email() As String
            Get
                Return _email
            End Get

            Set(ByVal value As String)
                _email = value

            End Set
        End Property

        Private ReadOnly _phone As Global.System.Collections.Generic.List(Of people.person.phone_number) = New Global.System.Collections.Generic.List(Of people.person.phone_number)()

        <Global.ProtoBuf.ProtoMember(4, Name:="phone", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
         Public ReadOnly Property phone() As Global.System.Collections.Generic.List(Of people.person.phone_number)

            Get
                Return _phone
            End Get

        End Property

        Private ReadOnly _test_packed As Global.System.Collections.Generic.List(Of Integer) = New Global.System.Collections.Generic.List(Of Integer)()

        <Global.ProtoBuf.ProtoMember(5, Name:="test_packed", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
         Public ReadOnly Property test_packed() As Global.System.Collections.Generic.List(Of Integer)

            Get
                Return _test_packed
            End Get

        End Property

        Private _test_deprecated As Integer = 0
        <Global.ProtoBuf.ProtoMember(6, IsRequired:=False, Name:="test_deprecated", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
        <Global.System.ComponentModel.DefaultValue(CType(0, Integer))> _
        Public Property test_deprecated() As Integer
            Get
                Return _test_deprecated
            End Get

            Set(ByVal value As Integer)
                _test_deprecated = value

            End Set
        End Property

        Private _foreach As Integer = 0
        <Global.ProtoBuf.ProtoMember(7, IsRequired:=False, Name:="foreach", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
        <Global.System.ComponentModel.DefaultValue(CType(0, Integer))> _
        Public Property foreach() As Integer
            Get
                Return _foreach
            End Get

            Set(ByVal value As Integer)
                _foreach = value

            End Set
        End Property

        <Global.System.Serializable(), Global.ProtoBuf.ProtoContract(Name:="phone_number")> _
        Partial Public Class phone_number
            Implements Global.ProtoBuf.IExtensible

            Public Sub New()
            End Sub

            Private _number As String
            <Global.ProtoBuf.ProtoMember(1, IsRequired:=True, Name:="number", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
            Public Property number() As String
                Get
                    Return _number
                End Get

                Set(ByVal value As String)
                    _number = value

                End Set
            End Property

            Private _type As people.person.phone_type = people.person.phone_type.home
            <Global.ProtoBuf.ProtoMember(2, IsRequired:=False, Name:="type", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
            <Global.System.ComponentModel.DefaultValue(CType(people.person.phone_type.home, people.person.phone_type))> _
            Public Property type() As people.person.phone_type
                Get
                    Return _type
                End Get

                Set(ByVal value As people.person.phone_type)
                    _type = value

                End Set
            End Property

            Private extensionObject As Global.ProtoBuf.IExtension
            Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
                Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
            End Function
        End Class

        Public Enum phone_type
            mobile = 0
            home = 1
            work = 2
        End Enum

        Private extensionObject As Global.ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
            Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

    <Global.System.Serializable(), Global.ProtoBuf.ProtoContract(Name:="opaque_message_list")> _
    Partial Public Class opaque_message_list
        Implements Global.ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private ReadOnly _messages_list As Global.System.Collections.Generic.List(Of Byte()) = New Global.System.Collections.Generic.List(Of Byte())()

        <Global.ProtoBuf.ProtoMember(1, Name:="messages_list", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
         Public ReadOnly Property messages_list() As Global.System.Collections.Generic.List(Of Byte())

            Get
                Return _messages_list
            End Get

        End Property

        Private extensionObject As Global.ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
            Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

End Namespace