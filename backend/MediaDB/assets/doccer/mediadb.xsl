<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

  <xsl:output method="xml"
              version="1.0"
              indent="yes"
              encoding="utf-8"
              omit-xml-declaration="no"
              doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"
              doctype-public="-//W3C//DTD XHTML 1.0 Strict//EN" />

  <xsl:strip-space elements="*"/>

  <xsl:param name="SELF" select="substring(//member[1]/@name, 3)" />

  <!-- Match root -->
  <xsl:template match="/"><!-- {{{ -->
    <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="en" lang="en">
      <head>
	<title><xsl:value-of select="$SELF" /></title>
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<link rel="stylesheet" type="text/css" href="assets/docgen.css"/>
      </head>
      <body>
	<div id="wrapper">
	  <div id="header">
	    <h1><a href="index.html">MediaDB</a></h1>
	    <hr/>
	  </div>
	  <div id="body"><xsl:apply-templates /></div>
	</div>
      </body>
    </html>
  </xsl:template><!-- }}} -->
  
  <!-- Match doc -->
  <xsl:template match="doc"><!-- {{{ -->
    <div id="menu"><xsl:apply-templates select="menu" mode="menu" /></div>
    <div id="content"><xsl:apply-templates select="class"/></div>
    <br class="clear" />
  </xsl:template><!-- }}} -->
  
  <!-- Match index
       This template draws a list of all classes 
  -->
  <xsl:template match="index"><!-- {{{ -->
    <ul>
      <xsl:for-each select="class">
	<li>
	  <a href="{@short-name}.html"><xsl:value-of select="@short-name" /></a>
	</li>
      </xsl:for-each>
    </ul>
  </xsl:template><!-- }}} -->

  <!-- Match menu
       This template generates the menu 
  -->
  <xsl:template match="menu" mode="menu"><!-- {{{ -->
    <ul>
      <xsl:for-each select="class">
	<!--<xsl:sort select="@short-name" />-->
	<li>
	  <xsl:if test="@short-name = $SELF">
	    <xsl:attribute name="class">selected</xsl:attribute>
	  </xsl:if>
	  <a href="{@short-name}.html"><xsl:value-of select="@short-name"/></a>
	</li>
      </xsl:for-each>
    </ul>
  </xsl:template><!-- }}} -->
  
  <!-- Collects all unwanted nodes in menu mode -->
  <xsl:template match="*" mode="menu" />
  
  <!-- Match class
       This template draws the class definition
  -->
  <xsl:template match="class"><!-- {{{ -->
    <div class="class">
      <div class="definition">
	<xsl:apply-templates select="member[position() = 1]" />
      </div>
      <div class="summary">
	<xsl:if test="count(member[@type = 'property']) &gt; 0">
	  <div class="members">
	    <h2 class="header">&#187; Properties</h2>
	    <xsl:apply-templates select="member[@type = 'property']" />
	  </div>
	</xsl:if>
	<xsl:if test="count(member[@type = 'field']) &gt; 0">
	  <div class="members">
	    <h2 class="header">&#187; Variables</h2>
	    <xsl:apply-templates select="member[@type = 'field']" />
	  </div>
	</xsl:if>
	<xsl:if test="count(member[@type = 'method']) &gt; 0">
	  <div class="members">
	    <h2 class="header">&#187; Methods</h2>
	    <xsl:apply-templates select="member[@type = 'method']" />
	  </div>
	</xsl:if>
	<xsl:if test="count(member[@type = 'event']) &gt; 0">
	  <div class="members">
	    <h2 class="header">&#187; Events</h2>
	    <xsl:apply-templates select="member[@type = 'event']" />
	  </div>
	</xsl:if>
      </div>
    </div>
  </xsl:template><!-- }}} -->
  
  <!-- Match the class definition
       This template matches the first member node which is the
       constructor of the class 
  -->
  <xsl:template match="member[position() = 1]"><!-- {{{ -->
    <h1><xsl:value-of select="@short-name" /></h1>
    <xsl:if test="string-length(summary)"><xsl:apply-templates /></xsl:if>
  </xsl:template><!-- }}} -->
  
  <!-- Match a class member -->
  <xsl:template match="member[position() &gt; 1]"><!-- {{{ -->
    <div class="member" id="{@id}">
      <xsl:if test="position() = last()">
	<xsl:attribute name="class">member last</xsl:attribute>
      </xsl:if>
      <h3>
	<xsl:for-each select="canonical-names/type">
	  <span class="property-{@type}"><xsl:value-of select="@name" /></span>
	  <xsl:if test="position() != last()">.</xsl:if>
	</xsl:for-each>
	<xsl:if test="@type = 'method'">
	  <xsl:choose>
	    <xsl:when test="param">(<xsl:apply-templates select="param" mode="param" />);</xsl:when>
	    <xsl:otherwise>();</xsl:otherwise>
	  </xsl:choose>
	</xsl:if>
      </h3>
      <xsl:apply-templates />
    </div>
  </xsl:template><!-- }}} -->

  <!-- Match params -->
  <xsl:template match="param"><!-- {{{ -->
    <div class="method-info">
      <span class="label" id="{../@id}-{@name}">Param: </span>
      <xsl:call-template name="draw-param" />
    </div>
    <xsl:if test="position() = last()">
      <br/>
    </xsl:if>
  </xsl:template><!-- }}} -->
  
  <!-- Match returns -->
  <xsl:template match="returns"><!-- {{{ -->
    <xsl:if test="string-length(.)">
      <div class="method-info">
	<span class="label">Returns: </span>
	<xsl:apply-templates />
      </div>
    </xsl:if>
  </xsl:template><!-- }}} -->
  
  <!-- Match paramref -->
  <xsl:template match="paramref"><!-- {{{ -->
    <a href="#{../../@id}-{@name}">
      <xsl:choose>
	<xsl:when test="string-length(.)"><xsl:apply-templates /></xsl:when>
	<xsl:otherwise><xsl:value-of select="@name" /></xsl:otherwise>
      </xsl:choose>
    </a>
  </xsl:template><!-- }}} -->

  <!-- Match param in param mode -->
  <xsl:template match="param" mode="param"><!-- {{{ -->
    <xsl:call-template name="draw-param" />
    <xsl:if test="position() != last()">, </xsl:if>
  </xsl:template><!-- }}} -->
  
  <!-- Draw parameter -->
  <xsl:template name="draw-param"><!-- {{{ -->
    <xsl:variable name="type" select="translate(@type, '{}','&lt;&gt;')" />
    <xsl:choose>
      <xsl:when test="not(starts-with(@type, 'System.'))">
	<a href="{@type}.html" class="data-type"><xsl:value-of select="$type"/></a>
      </xsl:when>
      <xsl:otherwise>
	<span class="data-type"><xsl:value-of select="$type"/></span>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:text> </xsl:text>
    <span class="param-name"><xsl:value-of select="@name"/></span>
  </xsl:template><!-- }}} -->
  
  <!-- Match summary -->
  <xsl:template match="summary"><!-- {{{ -->
    <xsl:choose>
      <xsl:when test="string-length(para)">
	<xsl:apply-templates select="para" />
      </xsl:when>
      <xsl:otherwise>
	<p><xsl:apply-templates /></p>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template><!-- }}} -->
  
  <!-- Match para -->
  <xsl:template match="para"><!-- {{{ -->
    <p><xsl:apply-templates /></p>
  </xsl:template><!-- }}} -->

  <!-- Match see -->
  <xsl:template match="see[@cref]"><!-- {{{ -->
    <xsl:variable name="cref" select="translate(@cref, 'T:','')" />
    <xsl:variable name="contents">
      <xsl:choose>
	<xsl:when test="string-length(.)"><xsl:apply-templates /></xsl:when>
	<xsl:otherwise><xsl:value-of select="$cref" /></xsl:otherwise>
      </xsl:choose>
    </xsl:variable>

    <xsl:choose>
      <xsl:when test="starts-with($cref, 'System.')">
	<span class="code-element"><xsl:value-of select="$contents" /></span>
      </xsl:when>
      <xsl:otherwise>
	<a href="{$cref}.html" class="code-element">
	   <xsl:value-of select="$contents" />
	</a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template><!-- }}} -->
  
  <!-- Match all 
       This templates matches all nodes and passes them along as-is
  -->
  <xsl:template match="*"><!-- {{{ -->
    <xsl:copy>
      <xsl:copy-of select="@*"/>
      <xsl:apply-templates />
    </xsl:copy>
  </xsl:template><!-- }}} -->

  <!-- Dump box -->
  <xsl:template match="canonical-names" />
  
</xsl:stylesheet>