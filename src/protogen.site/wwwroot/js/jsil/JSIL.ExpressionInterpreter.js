/* It is auto-generated file. Do not modify it. */
"use strict";
(function () {
  var $interpeter = JSIL.GetAssembly("JSIL.ExpressionInterpreter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

  JSIL.ImplementExternals("System.Linq.Expressions.LambdaExpression", function ($) {
    $.Method({Static:false , Public:true}, "Compile",
      JSIL.MethodSignature.Return($jsilcore.TypeRef("System.Delegate")),
      function Compile () {
        return $interpeter.Microsoft.Scripting.Generation.CompilerHelpers.LightCompile(this);
      }
    );
  });

  JSIL.ImplementExternals("System.Linq.Expressions.Expression`1", function ($) {
    $.Method({Static:false , Public:true}, "Compile",
      new JSIL.MethodSignature($.GenericParameter("TDelegate"), null),
      function Compile () {
        return $interpeter.Microsoft.Scripting.Generation.CompilerHelpers.LightCompile(this);
      }
    );
  });
})();