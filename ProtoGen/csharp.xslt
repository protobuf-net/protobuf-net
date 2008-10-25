<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="xsl msxsl"
>
  <xsl:output method="text" indent="no" omit-xml-declaration="yes"/>

  <xsl:template match="*">
    <xsl:message terminate="yes">
      Node not handled: <xsl:for-each select="ancestor-or-self::*">/<xsl:value-of select="name()"/></xsl:for-each>
      <xsl:for-each select="*">
        ; <xsl:value-of select="concat(name(),'=',.)"/>
      </xsl:for-each>
    </xsl:message>
  </xsl:template>
  
  <xsl:template match="FileDescriptorSet">
    <xsl:apply-templates select="file/FileDescriptorProto"/>
  </xsl:template>

  <xsl:template match="FileDescriptorProto">
    // see <xsl:value-of select="name"/>
    namespace <xsl:value-of select="package"/>
    {
      <xsl:apply-templates select="message_type/DescriptorProto"/>
    }
  </xsl:template>

  <xsl:template match="DescriptorProto">
    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class <xsl:value-of select="name"/>
    {
      <xsl:apply-templates select="*"/>
    }
  </xsl:template>

  <xsl:template match="DescriptorProto/name | DescriptorProto/extension_range | DescriptorProto/extension"/>
  
  <xsl:template match="DescriptorProto/field | DescriptorProto/enum_type | DescriptorProto/message_type
                | DescriptorProto/nested_type | EnumDescriptorProto/value">
    <xsl:apply-templates select="*"/>
  </xsl:template>

  <xsl:template match="EnumDescriptorProto">
    public enum <xsl:value-of select="name"/>
    {
      <xsl:apply-templates select="value"/>
    }
  </xsl:template>

  <xsl:template match="EnumValueDescriptorProto">
    <xsl:value-of select="concat(name,' = ',number)"/>
    <xsl:if test="position()!=last()">,
    </xsl:if>
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto" mode="type">
    <xsl:choose>
      <xsl:when test="type='TYPE_BOOL'">bool</xsl:when>
      <xsl:when test="type='TYPE_BYTES'">byte[]</xsl:when>
      <xsl:when test="type='TYPE_DOUBLE'">double</xsl:when>
      <xsl:when test="type='TYPE_UINT64'">ulong</xsl:when>
      <xsl:when test="type='TYPE_INT32' or not(type)">int</xsl:when>
      <xsl:when test="type='TYPE_INT64'">long</xsl:when>
      <xsl:when test="type='TYPE_STRING'">string</xsl:when>
      <xsl:when test="type='TYPE_MESSAGE' or type='TYPE_ENUM'"><xsl:value-of select="substring-after(type_name,'.')"/></xsl:when>
      <xsl:otherwise>
        <xsl:message terminate="yes">
          Field type not implemented: <xsl:value-of select="type"/> (<xsl:value-of select="../../name"/>.<xsl:value-of select="name"/>)
        </xsl:message>
      </xsl:otherwise>
    </xsl:choose>
    
  </xsl:template>

  <xsl:template match="FieldDescriptorProto[default_value]" mode="defaultValue">
    <xsl:choose>
      <xsl:when test="type='TYPE_STRING'">@"<xsl:value-of select="default_value"/>"</xsl:when>
      <xsl:when test="type='TYPE_ENUM'"><xsl:apply-templates select="." mode="type"/>.<xsl:value-of select="default_value"/></xsl:when>
      <xsl:otherwise><xsl:value-of select="default_value"/></xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!--
    We need to find the first enum value given .foo.bar.SomeEnum - but the enum itself
    only knows about SomeEnum; we need to look at all parent DescriptorProto nodes, and
    the FileDescriptorProto for the namespace.
    
    This does an annoying up/down recursion... a bit expensive, but *generally* OK.
    Could perhaps index the last part of the enum name to reduce overhead?
  -->
  <xsl:template name="GetFirstEnumValue">
    <xsl:variable name="hunt" select="type_name"/>
    <xsl:for-each select="//EnumDescriptorProto">
      <xsl:variable name="fullName">
        <xsl:for-each select="ancestor::FileDescriptorProto">.<xsl:value-of select="package"/></xsl:for-each>
        <xsl:for-each select="ancestor::DescriptorProto">.<xsl:value-of select="name"/></xsl:for-each>
        <xsl:value-of select="concat('.',name)"/>
      </xsl:variable>
      <xsl:if test="$fullName=$hunt"><xsl:value-of select="(value/EnumValueDescriptorProto)[1]/name"/></xsl:if>
    </xsl:for-each>
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[not(default_value)]" mode="defaultValue">
    <xsl:choose>
      <xsl:when test="type='TYPE_STRING'">""</xsl:when>
      <xsl:when test="type='TYPE_MESSAGE'">null</xsl:when>
      <xsl:when test="type='TYPE_BYTES'">null</xsl:when>
      <xsl:when test="type='TYPE_ENUM'"><xsl:apply-templates select="." mode="type"/>.<xsl:call-template name="GetFirstEnumValue"/></xsl:when>
      <xsl:otherwise>default(<xsl:apply-templates select="." mode="type"/>)</xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="FieldDescriptorProto[label='LABEL_OPTIONAL' or not(label)]">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    <xsl:variable name="defaultValue"><xsl:apply-templates select="." mode="defaultValue"/></xsl:variable>
    private <xsl:value-of select="concat($type, ' _', generate-id())"/> = <xsl:value-of select="$defaultValue"/>;

    [ProtoBuf.ProtoMember(<xsl:value-of select="number"/>)]
    [System.ComponentModel.DefaultValue(<xsl:value-of select="$defaultValue"/>)]
    public <xsl:value-of select="concat($type,' ',name)"/>
    {
      get { return _<xsl:value-of select="generate-id()"/>; }
      set { _<xsl:value-of select="generate-id()"/> = value; }
    }
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[label='LABEL_REQUIRED']">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    private <xsl:value-of select="concat($type, ' _', generate-id())"/>;

    [ProtoBuf.ProtoMember(<xsl:value-of select="number"/>)]
    public <xsl:value-of select="concat($type,' ',name)"/>
    {
      get { return _<xsl:value-of select="generate-id()"/>; }
      set { _<xsl:value-of select="generate-id()"/> = value; }
    }
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[label='LABEL_REPEATED']">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    private readonly System.Collections.Generic.List&lt;<xsl:value-of select="$type" />&gt; _<xsl:value-of select="generate-id()"/> = new System.Collections.Generic.List&lt;<xsl:value-of select="$type"/>&gt;();

    [ProtoBuf.ProtoMember(<xsl:value-of select="number"/>)]
    public System.Collections.Generic.List&lt;<xsl:value-of select="$type" />&gt; <xsl:value-of select="name"/>
    {
      get { return _<xsl:value-of select="generate-id()"/>; }
      set
      { // setter needed for XmlSerializer
        _<xsl:value-of select="generate-id()"/>.Clear();
        if(value != null)
        {
          _<xsl:value-of select="generate-id()"/>.AddRange(value);
        }
      }
    }
</xsl:template>
  
</xsl:stylesheet>
