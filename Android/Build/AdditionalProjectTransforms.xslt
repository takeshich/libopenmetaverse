<xsl:if test="Input/Generation/ProjectName = 'OpenMetaverse'">
  <Target Name="BeforeBuild">
    <Exec>
      <xsl:attribute name="WorkingDirectory">
        <xsl:text>jni</xsl:text>
      </xsl:attribute>
      <xsl:attribute name="Command">
          <xsl:text>$(AndroidNdkDirectory)\ndk-build</xsl:text>
      </xsl:attribute>
    </Exec>
  </Target>
</xsl:if>

