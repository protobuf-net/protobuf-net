
' Generated from 
' Generated from: ProtoGen/rpc.proto
Namespace ProtoGen.rpc
    <Global.System.Serializable(), Global.ProtoBuf.ProtoContract(Name:="SearchRequest")> _
    Partial Public Class SearchRequest
        Implements Global.ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private _query As String
        <Global.ProtoBuf.ProtoMember(1, IsRequired:=True, Name:="query", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
        Public Property query() As String
            Get
                Return _query
            End Get

            Set(ByVal value As String)
                _query = value

            End Set
        End Property

        Private _page_number As Integer = 0
        <Global.ProtoBuf.ProtoMember(2, IsRequired:=False, Name:="page_number", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
        <Global.System.ComponentModel.DefaultValue(CType(0, Integer))> _
        Public Property page_number() As Integer
            Get
                Return _page_number
            End Get

            Set(ByVal value As Integer)
                _page_number = value

            End Set
        End Property

        Private _result_per_page As Integer = 0
        <Global.ProtoBuf.ProtoMember(3, IsRequired:=False, Name:="result_per_page", DataFormat:=Global.ProtoBuf.DataFormat.TwosComplement)> _
        <Global.System.ComponentModel.DefaultValue(CType(0, Integer))> _
        Public Property result_per_page() As Integer
            Get
                Return _result_per_page
            End Get

            Set(ByVal value As Integer)
                _result_per_page = value

            End Set
        End Property

        Private extensionObject As Global.ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
            Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

    <Global.System.Serializable(), Global.ProtoBuf.ProtoContract(Name:="SearchResponse")> _
    Partial Public Class SearchResponse
        Implements Global.ProtoBuf.IExtensible

        Public Sub New()
        End Sub

        Private ReadOnly _result As Global.System.Collections.Generic.List(Of String) = New Global.System.Collections.Generic.List(Of String)()

        <Global.ProtoBuf.ProtoMember(1, Name:="result", DataFormat:=Global.ProtoBuf.DataFormat.Default)> _
         Public ReadOnly Property result() As Global.System.Collections.Generic.List(Of String)

            Get
                Return _result
            End Get

        End Property

        Private extensionObject As Global.ProtoBuf.IExtension
        Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject
            Return Global.ProtoBuf.Extensible.GetExtensionObject(extensionObject, createIfMissing)
        End Function
    End Class

    <Global.System.ServiceModel.ServiceContract(Name:="SearchService")> _
    Public Interface ISearchService
        Function Search(ByVal request As SearchRequest) As SearchResponse
    End Interface


End Namespace