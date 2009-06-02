<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:template name="PickNamespace"><xsl:param name="defaultNamespace"/><xsl:choose>
      <xsl:when test="package"><xsl:value-of select="package"/></xsl:when>
      <xsl:when test="$defaultNamespace"><xsl:value-of select="$defaultNamespace"/></xsl:when>
      <xsl:when test="substring(name,string-length(name)-5,6)='.proto'"><xsl:value-of select="substring(name,1,string-length(name)-6)"/></xsl:when>
      <xsl:otherwise><xsl:value-of select="name"/></xsl:otherwise>
    </xsl:choose></xsl:template>

  <!--
  <xsl:template name="capitalizeFirst">
    <xsl:param name="value"/>
    <xsl:value-of select="translate(substring($value,1,1),$alpha,$ALPHA)"/>
    <xsl:value-of select="substring($value,2)"/>
  </xsl:template>
  -->
  <xsl:template name="toCamelCase">
    <xsl:param name="value"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:variable name="segment" select="substring-before($value, $delimiter)"/>
    <xsl:choose>
      <xsl:when test="$segment != ''">
        <xsl:value-of select="$segment"/>
        <xsl:call-template name="toPascalCase">
          <xsl:with-param name="value" select="substring-after($value, $delimiter)"/>
          <xsl:with-param name="delimiter" select="$delimiter"/>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$value"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:variable name="alpha" select="'abcdefghijklmnopqrstuvwxyz'"/>
  <xsl:variable name="ALPHA" select="'ABCDEFGHIJKLMNOPQRSTUVWXYZ'"/>

  <xsl:template name="toPascalCase">
    <xsl:param name="value"/>
    <xsl:param name="delimiter" select="'_'"/>
    <xsl:if test="$value != ''">
      <xsl:variable name="segment" select="substring-before($value, $delimiter)"/>
      <xsl:choose>
        <xsl:when test="$segment != ''">
          <xsl:value-of select="translate(substring($segment,1,1),$alpha,$ALPHA)"/><xsl:value-of select="substring($segment,2)"/>
          <xsl:call-template name="toPascalCase">
            <xsl:with-param name="value" select="substring-after($value, $delimiter)"/>
            <xsl:with-param name="delimiter" select="$delimiter"/>
          </xsl:call-template>    
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="translate(substring($value,1,1),$alpha,$ALPHA)"/><xsl:value-of select="substring($value,2)"/>
        </xsl:otherwise>
      </xsl:choose>      
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>
