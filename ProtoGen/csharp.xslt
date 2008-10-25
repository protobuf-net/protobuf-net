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
      using ProtoBuf;
      using System.Collections.Generic;
      
      <xsl:apply-templates select="message_type/DescriptorProto"/>
    }
  </xsl:template>

  <xsl:template match="DescriptorProto">
    [ProtoContract]
    public class <xsl:value-of select="name"/>
    {
      <xsl:apply-templates select="field/FieldDescriptorProto | nested_type/DescriptorProto"/>
    }
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

  <xsl:template match="FieldDescriptorProto[label='LABEL_REQUIRED' or not(label)]">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    private <xsl:value-of select="concat($type, ' ', generate-id())"/>;

    [ProtoMember(<xsl:value-of select="number"/>)]
    public <xsl:value-of select="concat($type,' ',name)"/>
    {
      get { return <xsl:value-of select="generate-id()"/>; }
      set { <xsl:value-of select="generate-id()"/> = value; }
    }
  </xsl:template>
  
  <xsl:template match="FieldDescriptorProto[label='LABEL_REPEATED']">
    <xsl:variable name="type"><xsl:apply-templates select="." mode="type"/></xsl:variable>
    private readonly List&lt;<xsl:value-of select="$type" />&gt; <xsl:value-of select="generate-id()"/> = new List&lt;<xsl:value-of select="$type"/>&gt;();

    [ProtoMember(<xsl:value-of select="number"/>)]
    public <xsl:value-of select="concat($type,' ',name)"/>
    {
      get { return <xsl:value-of select="generate-id()"/>; }
    }
  </xsl:template>
  
</xsl:stylesheet>
