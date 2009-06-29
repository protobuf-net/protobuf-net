
' Generated from 
' Generated from: ProtoGen/person.proto
Namespace people
    <System.Serializable(), ProtoBuf.ProtoContract(Name:="person")> _
    Partial Public Class person
        Implements ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private _name As String
        <ProtoBuf.ProtoMember(1, IsRequired:=True, Name:="name", DataFormat:=ProtoBuf.DataFormat.Default)> _
        Public Property name() As String
            Get
                Return _name
            End Get

            Set(ByVal value As String)
                _name = value

            End Set
        End Property

        Private _id As Integer = 0
        <ProtoBuf.ProtoMember(2, IsRequired:=False, Name:="id", DataFormat:=ProtoBuf.DataFormat.TwosComplement)> _
        <System.ComponentModel.DefaultValue(GetType(Integer), "0")> _
        Public Property id() As Integer
            Get
                Return _id
            End Get

            Set(ByVal value As Integer)
                _id = value

            End Set
        End Property

        Private _email As String = ""
        <ProtoBuf.ProtoMember(3, IsRequired:=False, Name:="email", DataFormat:=ProtoBuf.DataFormat.Default)> _
        <System.ComponentModel.DefaultValue(GetType(String), """")> _
        Public Property email() As String
            Get
                Return _email
            End Get

            Set(ByVal value As String)
                _email = value

            End Set
        End Property

        Private ReadOnly _phone As System.Collections.Generic.List(Of people.person.phone_number) = New System.Collections.Generic.List(Of people.person.phone_number)()

        <ProtoBuf.ProtoMember(4, Name:="phone", DataFormat:=ProtoBuf.DataFormat.Default)> _
         Public ReadOnly Property phone() As System.Collections.Generic.List(Of people.person.phone_number)

            Get
                Return _phone
            End Get

        End Property

        Private ReadOnly _test_packed As System.Collections.Generic.List(Of Integer) = New System.Collections.Generic.List(Of Integer)()

        <ProtoBuf.ProtoMember(5, Name:="test_packed", DataFormat:=ProtoBuf.DataFormat.TwosComplement)> _
         Public ReadOnly Property test_packed() As System.Collections.Generic.List(Of Integer)

            Get
                Return _test_packed
            End Get

        End Property

        Private _test_deprecated As Integer = 0
        <ProtoBuf.ProtoMember(6, IsRequired:=False, Name:="test_deprecated", DataFormat:=ProtoBuf.DataFormat.TwosComplement)> _
        <System.ComponentModel.DefaultValue(GetType(Integer), "0")> _
        Public Property test_deprecated() As Integer
            Get
                Return _test_deprecated
            End Get

            Set(ByVal value As Integer)
                _test_deprecated = value

            End Set
        End Property

        Private _foreach As Integer = 0
        <ProtoBuf.ProtoMember(7, IsRequired:=False, Name:="foreach", DataFormat:=ProtoBuf.DataFormat.TwosComplement)> _
        <System.ComponentModel.DefaultValue(GetType(Integer), "0")> _
        Public Property foreach() As Integer
            Get
                Return _foreach
            End Get

            Set(ByVal value As Integer)
                _foreach = value

            End Set
        End Property

        <System.Serializable(), ProtoBuf.ProtoContract(Name:="phone_number")> _
        Partial Public Class phone_number
            Implements ProtoBuf.IExtensible

            Public Sub New()
            End Sub

            Private _number As String
            <ProtoBuf.ProtoMember(1, IsRequired:=True, Name:="number", DataFormat:=ProtoBuf.DataFormat.Default)> _
            Public Property number() As String
                Get
                    Return _number
                End Get

                Set(ByVal value As String)
                    _number = value

                End Set
            End Property

            Private _type As people.person.phone_type = people.person.phone_type.home
            <ProtoBuf.ProtoMember(2, IsRequired:=False, Name:="type", DataFormat:=ProtoBuf.DataFormat.TwosComplement)> _
            <System.ComponentModel.DefaultValue(GetType(people.person.phone_type), "people.person.phone_type.home")> _
            Public Property type() As people.person.phone_type
                Get
                    Return _type
                End Get

                Set(ByVal value As people.person.phone_type)
                    _type = value

                End Set
            End Property

            Private extensionObject As ProtoBuf.IExtension
            Function GetExtensionObject(ByVal createIfMissing As Boolean) As ProtoBuf.IExtension Implements ProtoBuf.IExtensible.GetExtensionObject
                Return ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
            End Function
        End Class

        Public Enum phone_type
            mobile = 0
            home = 1
            work = 2
        End Enum

        Private extensionObject As ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As ProtoBuf.IExtension Implements ProtoBuf.IExtensible.GetExtensionObject
            Return ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

    <System.Serializable(), ProtoBuf.ProtoContract(Name:="opaque_message_list")> _
    Partial Public Class opaque_message_list
        Implements ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private ReadOnly _messages_list As System.Collections.Generic.List(Of Byte()) = New System.Collections.Generic.List(Of Byte())()

        <ProtoBuf.ProtoMember(1, Name:="messages_list", DataFormat:=ProtoBuf.DataFormat.Default)> _
         Public ReadOnly Property messages_list() As System.Collections.Generic.List(Of Byte())

            Get
                Return _messages_list
            End Get

        End Property

        Private extensionObject As ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As ProtoBuf.IExtension Implements ProtoBuf.IExtensible.GetExtensionObject
            Return ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

End Namespace