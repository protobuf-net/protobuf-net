/* It is auto-generated file. Do not modify it. */
if (typeof (JSIL) === "undefined")
    throw new Error("JSIL.Core is required");

if (!$jsilcore)
    throw new Error("JSIL.Core is required");

JSIL.DeclareNamespace("System.Linq");
JSIL.DeclareNamespace("System.Linq.Expressions");

JSIL.MakeClass("System.Object", "JSIL.AbstractEnumerable", true, ["T"], function ($) {
    var T = new JSIL.GenericParameter("T", "JSIL.AbstractEnumerable");

    $.Method({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [JSIL.AnyType, JSIL.AnyType, JSIL.AnyType]),
      function (getNextItem, reset, dispose) {
          if (arguments.length === 1) {
              this._getEnumerator = getNextItem;
          } else {
              this._getEnumerator = null;
              this._getNextItem = getNextItem;
              this._reset = reset;
              this._dispose = dispose;
          }
      }
    );

    function getEnumeratorImpl() {
        if (this._getEnumerator !== null)
            return this._getEnumerator();
        else
            return new (JSIL.AbstractEnumerator.Of(this.T))(this._getNextItem, this._reset, this._dispose);
    };

    $.Method({ Static: false, Public: false }, null,
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.IEnumerator"), []),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.IEnumerable", "GetEnumerator");

    $.Method({ Static: false, Public: true }, "GetEnumerator",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerator`1", [T]), []),
      getEnumeratorImpl
    )
      .Overrides("System.Collections.Generic.IEnumerable`1", "GetEnumerator");

    $.ImplementInterfaces(
      /* 0 */ $jsilcore.TypeRef("System.Collections.IEnumerable"),
      /* 1 */ $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [T])
    );
});
JSIL.ImplementExternals(
  "System.Linq.Enumerable", function ($) {
      $.Method({ Static: true, Public: true }, "Any",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"]),
        function (T, enumerable) {
            var enumerator = JSIL.GetEnumerator(enumerable, T);

            var moveNext = System.Collections.IEnumerator.MoveNext;

            try {
                if (moveNext.Call(enumerator))
                    return true;
            } finally {
                JSIL.Dispose(enumerator);
            }

            return false;
        }
      );

      $.Method({ Static: true, Public: true }, "Any",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Boolean"),
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", $jsilcore.TypeRef("System.Boolean")])],
          ["TSource"]
        ),
        function (T, enumerable, predicate) {
            var enumerator = JSIL.GetEnumerator(enumerable, T);

            var tIEnumerator = System.Collections.Generic.IEnumerator$b1.Of(T);
            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = tIEnumerator.get_Current;

            try {
                while (moveNext.Call(enumerator)) {
                    if (predicate(get_Current.Call(enumerator)))
                        return true;
                }
            } finally {
                JSIL.Dispose(enumerator);
            }

            return false;
        }
      );

      $.Method({ Static: true, Public: true }, "Count",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"]),
        function (T, enumerable) {
            var enumerator = JSIL.GetEnumerator(enumerable, T);

            var moveNext = System.Collections.IEnumerator.MoveNext;

            var result = 0;
            try {
                while (moveNext.Call(enumerator))
                    result += 1;
            } finally {
                JSIL.Dispose(enumerator);
            }
            return result;
        }
      );

      var elementAtImpl = function (enumerable, index) {
          var e = JSIL.GetEnumerator(enumerable);

          var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
          var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;

          try {
              while (moveNext.Call(e)) {
                  if (index === 0) {
                      return { success: true, value: getCurrent.Call(e) };
                  } else {
                      index -= 1;
                  }
              }
          } finally {
              JSIL.Dispose(e);
          }

          return { success: false };
      };

      var firstImpl = function (enumerable, predicate) {
          var e = JSIL.GetEnumerator(enumerable);

          var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
          var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;

          try {
              if (arguments.length >= 2) {
                  while (moveNext.Call(e)) {
                      var value = getCurrent.Call(e);
                      if (predicate(value))
                          return { success: true, value: value };
                  }
              } else {
                  if (moveNext.Call(e))
                      return { success: true, value: getCurrent.Call(e) };
              }
          } finally {
              JSIL.Dispose(e);
          }

          return { success: false };
      };

      $.Method({ Static: true, Public: true }, "First",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])],
          ["TSource"]
        ),
        function (T, enumerable) {
            var result = firstImpl(enumerable);
            if (!result.success)
                throw new System.InvalidOperationException("Sequence contains no elements");

            return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "FirstOrDefault",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])],
          ["TSource"]
        ),
        function (T, enumerable) {
            var result = firstImpl(enumerable);
            if (!result.success)
                return JSIL.DefaultValue(T);

            return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "First",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]),
           $jsilcore.TypeRef("System.Func`2", ["!!0", $.Boolean])],
          ["TSource"]
        ),
        function (T, enumerable, predicate) {
            var result = firstImpl(enumerable, predicate);
            if (!result.success)
                throw new System.InvalidOperationException("Sequence contains no elements");

            return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "FirstOrDefault",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]),
           $jsilcore.TypeRef("System.Func`2", ["!!0", $.Boolean])],
          ["TSource"]
        ),
        function (T, enumerable, predicate) {
            var result = firstImpl(enumerable, predicate);
            if (!result.success)
                return JSIL.DefaultValue(T);

            return result.value;
        }
      );

      var lastImpl = function (T, enumerable, predicate) {
        var IList = System.Collections.Generic.IList$b1.Of(T);
        var list = IList.$As(enumerable);
        if (list !== null) {
          var item = IList.get_Item;
          var useLength = (typeof list.Count) == 'undefined';
          if ((useLength && list.length === 0) || list.Count === 0)
            return { success: false };
          var len = useLength ? list.length : list.Count;
          if (arguments.length >= 3) {
            for (var i = len - 1; i >= 0; i--) {
              var val = item.Call(list, [], i);
              if (predicate(val))
                return {
                  success: true,
                  value: val
                }
            }
            return { success: false };
          } else {
            return {
              success: true,
              value: item.Call(list, [], len - 1)
            };
          }
        }
        var e = JSIL.GetEnumerator(enumerable);

        var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
        var getCurrent = $jsilcore.System.Collections.IEnumerator.get_Current;

        try {
          var acceptedVal;
          var val;
          while (moveNext.Call(e)) {
              val = getCurrent.Call(e);
              if (arguments.length >= 3) {
                if (predicate(val))
                  acceptedVal = val;
              } else
                acceptedVal = val;
          }
          if (typeof acceptedVal !== 'undefined')
            return { success: true, value: acceptedVal };
          return { success: false };
        } finally {
          JSIL.Dispose(e);
        }
      };

      $.Method({ Static: true, Public: true }, "Last",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
            ["!!0"])],
          ["TSource"]
        ),
        function (T, enumerable) {
          var result = lastImpl(T, enumerable);
          if (!result.success)
              throw new System.InvalidOperationException("Sequence contains no elements");

          return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "Last",
        new JSIL.MethodSignature(
          "!!0",
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]),
           $jsilcore.TypeRef("System.Func`2", ["!!0", $.Boolean])],
          ["TSource"]
        ),
        function (T, enumerable, predicate) {
            var result = lastImpl(T, enumerable, predicate);
            if (!result.success)
                throw new System.InvalidOperationException("Sequence contains no elements");

            return result.value;
        }
      );


      $.Method({ Static: true, Public: true }, "Select",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!1"]),
          [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", "!!1"])],
          ["TSource", "TResult"]
        ),
        function (TSource, TResult, enumerable, selector) {
            var state = {};

            var tIEnumerator = System.Collections.Generic.IEnumerator$b1.Of(TSource);
            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = tIEnumerator.get_Current;

            return new (JSIL.AbstractEnumerable.Of(TResult))(
              function getNext(result) {
                  var ok = moveNext.Call(state.enumerator);
                  if (ok)
                      result.set(selector(get_Current.Call(state.enumerator)));

                  return ok;
              },
              function reset() {
                  state.enumerator = JSIL.GetEnumerator(enumerable, TSource);
              },
              function dispose() {
                  JSIL.Dispose(state.enumerator);
              }
            );
        }
      );

      $.Method({ Static: true, Public: true }, "SelectMany",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!1"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!1"])])], ["TSource", "TResult"]),
        function SelectMany$b2(TSource, TResult, source, selector) {
            var state = {
                enumerator: null,
                currentSubsequence: null
            };

            var tIEnumerator = System.Collections.Generic.IEnumerator$b1.Of(TSource);
            var tIEnumeratorResult = System.Collections.Generic.IEnumerator$b1.Of(TResult);
            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = tIEnumerator.get_Current;
            var get_CurrentResult = tIEnumeratorResult.get_Current;

            return new (JSIL.AbstractEnumerable.Of(TResult))(
              function getNext(result) {
                  while (true) {
                      if (state.currentSubsequence !== null) {
                          var ok = moveNext.Call(state.currentSubsequence);
                          if (ok) {
                              result.set(get_CurrentResult.Call(state.currentSubsequence));
                              return ok;
                          } else {
                              state.currentSubsequence = null;
                              JSIL.Dispose(state.currentSubsequence);
                          }
                      }

                      var ok = moveNext.Call(state.enumerator);
                      if (ok) {
                          var enumerable = selector(get_Current.Call(state.enumerator));
                          state.currentSubsequence = JSIL.GetEnumerator(enumerable, TResult);
                      } else {
                          return ok;
                      }
                  }
              },
              function reset() {
                  state.enumerator = JSIL.GetEnumerator(source, TSource);
                  state.currentSubsequence = null;
              },
              function dispose() {
                  JSIL.Dispose(state.enumerator);
                  JSIL.Dispose(state.currentSubsequence);
              }
            );
        }
      );

      $.Method({ Static: true, Public: true }, "Zip",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
            ["!!2"]),
          [
            $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
              ["!!0"]),
            $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
              ["!!1"]),
            $jsilcore.TypeRef("System.Func`3", ["!!0", "!!1", "!!2"])
          ],
          ["TFirst", "TSecond", "TResult"]
        ),
        function (TFirst, TSecond, TResult, first, second, combiner) {
          var state = {};

          var tIEnumerator1 = System.Collections.Generic.IEnumerator$b1.Of(TFirst);
          var tIEnumerator2 = System.Collections.Generic.IEnumerator$b1.Of(TSecond);
          var moveNext = System.Collections.IEnumerator.MoveNext;
          var get_Current1 = tIEnumerator1.get_Current;
          var get_Current2 = tIEnumerator2.get_Current;

          return new (JSIL.AbstractEnumerable.Of(TResult))(
            function getNext(result) {
                var ok1 = moveNext.Call(state.enumerator1);
                var ok2 = moveNext.Call(state.enumerator2);
                if (ok1 && ok2)
                    result.set(combiner(get_Current1.Call(state.enumerator1),
                                get_Current2.Call(state.enumerator2)));

                return ok1 && ok2;
            },
            function reset() {
                state.enumerator1 = JSIL.GetEnumerator(first, TFirst);
                state.enumerator2 = JSIL.GetEnumerator(second, TSecond);
            },
            function dispose() {
                JSIL.Dispose(state.enumerator1);
                JSIL.Dispose(state.enumerator2);
            }
          );
        }
      );

      $.Method({ Static: true, Public: true }, "ToArray",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", ["!!0"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"]),
        function (T, enumerable) {
            return JSIL.EnumerableToArray(enumerable, T);
        }
      );

      $.Method({ Static: true, Public: true }, "Skip",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
            ["!!0"]),
          [
            $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1",
              ["!!0"]),
            "System.Int32"
          ],
          ["TSource"]
        ),
        function (TSource, source, count) {
          var state = {};

          var tIEnumerator = System.Collections.Generic.IEnumerator$b1.Of(TSource);
          var moveNext = System.Collections.IEnumerator.MoveNext;
          var get_Current = tIEnumerator.get_Current;

          return new (JSIL.AbstractEnumerable.Of(TSource))(
            function getNext(result) {
                if (!state.ready) {
                  for (var i = 0; i < count; i++)
                    moveNext.Call(state.enumerator);
                  state.ready = true;
                }
                var ok = moveNext.Call(state.enumerator);
                if (ok)
                    result.set(get_Current.Call(state.enumerator));
                return ok;
            },
            function reset() {
                state.enumerator = JSIL.GetEnumerator(source, TSource);
            },
            function dispose() {
                JSIL.Dispose(state.enumerator);
            }
          );
        }
      );

      $.Method({ Static: true, Public: true }, "Contains",
        (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), "!!0"], ["TSource"])),
        function Contains$b1(T, source, item) {
            var enumerator = JSIL.GetEnumerator(source, T);

            var tIEnumerator = System.Collections.Generic.IEnumerator$b1.Of(T);
            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = tIEnumerator.get_Current;

            try {
                while (moveNext.Call(enumerator)) {
                    if (JSIL.ObjectEquals(get_Current.Call(enumerator), item))
                        return true;
                }
            } finally {
                JSIL.Dispose(enumerator);
            }

            return false;
        }
      );

      $.Method({ Static: true, Public: true }, "Cast",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]),
          [$jsilcore.TypeRef("System.Collections.IEnumerable")],
          ["TResult"]
        ),
       function (TResult, enumerable) {
           var state = {};

           var moveNext = System.Collections.IEnumerator.MoveNext;
           var get_Current = System.Collections.IEnumerator.get_Current;

           return new (JSIL.AbstractEnumerable.Of(TResult))(
             function getNext(result) {
                 var ok = moveNext.Call(state.enumerator);

                 if (ok)
                     result.set(TResult.$Cast(get_Current.Call(state.enumerator)));

                 return ok;
             },
             function reset() {
                 state.enumerator = JSIL.GetEnumerator(enumerable);
             },
             function dispose() {
                 JSIL.Dispose(state.enumerator);
             }
           );
       });

      $.Method({ Static: true, Public: true }, "Empty",
        new JSIL.MethodSignature(
          $jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]),
          [],
          ["TResult"]
        ),
       function (TResult) {
           return new (JSIL.AbstractEnumerable.Of(TResult))(
             function getNext(result) {
                 return false;
             },
             function reset() {
             },
             function dispose() {
             }
           );
       });

      $.Method({ Static: true, Public: true }, "ToList",
        new JSIL.MethodSignature(
         $jsilcore.TypeRef("System.Collections.Generic.List`1", ["!!0"]),
         [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])],
         ["TSource"]
      ),
      function (TSource, enumerable) {
          var constructor = new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Collections.Generic.List`1", [TSource]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [TSource])]);
          return constructor.Construct(enumerable);
      });

      $.Method({ Static: true, Public: true }, "ElementAt",
        new JSIL.MethodSignature("!!0", [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $.Int32], ["TSource"]),
        function ElementAt$b1(TSource, source, index) {
            var result = elementAtImpl(source, index);
            if (!result.success)
                throw new System.ArgumentOutOfRangeException("index");

            return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "ElementAtOrDefault",
        new JSIL.MethodSignature("!!0", [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $.Int32], ["TSource"]),
        function ElementAtOrDefault$b1(TSource, source, index) {
            var result = elementAtImpl(source, index);
            if (!result.success)
                return JSIL.DefaultValue(TSource);

            return result.value;
        }
      );

      $.Method({ Static: true, Public: true }, "OfType",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), [$jsilcore.TypeRef("System.Collections.IEnumerable")], ["TResult"]),
        function OfType$b1(TResult, source) {
            var state = {};

            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = System.Collections.IEnumerator.get_Current;

            return new (JSIL.AbstractEnumerable.Of(TResult))(
              function getNext(result) {
                  while (moveNext.Call(state.enumerator)) {
                      var current = get_Current.Call(state.enumerator);

                      if (TResult.$Is(current)) {
                          result.set(current);
                          return true;
                      }
                  }

                  return false;
              },
              function reset() {
                  state.enumerator = JSIL.GetEnumerator(source);
              },
              function dispose() {
                  JSIL.Dispose(state.enumerator);
              }
            );
        }
      );

      $.Method({ Static: true, Public: true }, "Where",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", $.Boolean])], ["TSource"]),
        function Where$b1(TSource, source, predicate) {
            var state = {};

            var moveNext = System.Collections.IEnumerator.MoveNext;
            var get_Current = System.Collections.IEnumerator.get_Current;

            return new (JSIL.AbstractEnumerable.Of(TSource))(
              function getNext(result) {
                  while (moveNext.Call(state.enumerator)) {
                      var current = get_Current.Call(state.enumerator);

                      if (predicate(current)) {
                          result.set(current);
                          return true;
                      }
                  }

                  return false;
              },
              function reset() {
                  state.enumerator = JSIL.GetEnumerator(source);
              },
              function dispose() {
                  JSIL.Dispose(state.enumerator);
              }
            );
        }
      );

      $.Method({ Static: true, Public: true }, "Range",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$.Int32]), [$.Int32, $.Int32], []),
        function Range(start, count) {
            var nextValue = start;
            var left = count;

            return new (JSIL.AbstractEnumerable.Of(System.Int32.__Type__))(
              function getNext(result) {
                  if (left <= 0)
                      return false;

                  result.set(nextValue);

                  nextValue += 1;
                  left -= 1;

                  return true;
              },
              function reset() {
                  nextValue = start;
                  left = count;
              },
              function dispose() {
              }
            );
        }
      );

      $.Method({ Static: true, Public: true }, "Sum",
        new JSIL.MethodSignature(
         $.Int32,
         [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$.Int32])],
         []
      ),
      function Sum_Int32(enumerable) {
          var result = 0;

          var e = JSIL.GetEnumerator(enumerable, $jsilcore.System.Int32);

          var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
          var getCurrent = $jsilcore.System.Collections.Generic.IEnumerator$b1.Of($jsilcore.System.Int32).get_Current;

          try {
              while (moveNext.Call(e)) {
                  result = (result + getCurrent.Call(e)) | 0;
              }
          } finally {
              JSIL.Dispose(e);
          }

          return result;
      });

      $.Method({ Static: true, Public: true }, "Sum",
        new JSIL.MethodSignature(
         $.Single,
         [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$.Single])],
         []
      ),
      function Sum_Single(enumerable) {
          var result = +0;

          var e = JSIL.GetEnumerator(enumerable, $jsilcore.System.Single);

          var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
          var getCurrent = $jsilcore.System.Collections.Generic.IEnumerator$b1.Of($jsilcore.System.Single).get_Current;

          try {
              while (moveNext.Call(e)) {
                  result = +(result + getCurrent.Call(e));
              }
          } finally {
              JSIL.Dispose(e);
          }

          return result;
      });

      $.Method({ Static: true, Public: true }, "Sum",
        new JSIL.MethodSignature(
         $.Double,
         [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", [$.Double])],
         []
      ),
      function Sum_Double(enumerable) {
          var result = +0;

          var e = JSIL.GetEnumerator(enumerable, $jsilcore.System.Double);

          var moveNext = $jsilcore.System.Collections.IEnumerator.MoveNext;
          var getCurrent = $jsilcore.System.Collections.Generic.IEnumerator$b1.Of($jsilcore.System.Double).get_Current;

          try {
              while (moveNext.Call(e)) {
                  result = +(result + getCurrent.Call(e));
              }
          } finally {
              JSIL.Dispose(e);
          }

          return result;
      });

      $.Method({ Static: true, Public: true }, "AsEnumerable",
        new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"]),
        function AsEnumerable$b1(TSource, source) {
            return source;
        }
      );
  }
);

JSIL.MakeStaticClass("System.Linq.Enumerable", true, [], function ($) {
    $.ExternalMethod({ Static: true, Public: true }, "Any",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"])
    );

    $.ExternalMethod({ Static: true, Public: true }, "Any",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", $jsilcore.TypeRef("System.Boolean")])], ["TSource"])
    );

    $.ExternalMethod({ Static: true, Public: true }, "Count",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Int32"), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"])
    );

    $.ExternalMethod({ Static: true, Public: true }, "First",
      new JSIL.MethodSignature("!!0", [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"])
    );

    $.ExternalMethod({ Static: true, Public: true }, "Select",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!1"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"]), $jsilcore.TypeRef("System.Func`2", ["!!0", "!!1"])], ["TSource", "TResult"])
    );

    $.ExternalMethod({ Static: true, Public: true }, "ToArray",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Array", ["!!0"]), [$jsilcore.TypeRef("System.Collections.Generic.IEnumerable`1", ["!!0"])], ["TSource"])
    );
});

JSIL.MakeClass($jsilcore.TypeRef("System.Object"), "System.Linq.Expressions.Expression", true, [], function ($) {
    var $thisType = $.publicInterface;

    $.ExternalMethod({ Static: true, Public: true }, "Constant",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), [$.Object], []))
    );

    $.ExternalMethod({ Static: true, Public: true }, "Constant",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), [$.Object, $jsilcore.TypeRef("System.Type")], []))
    );

    $.ExternalMethod({ Static: true, Public: true }, "Lambda",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.Expression`1", ["!!0"]), [$jsilcore.TypeRef("System.Linq.Expressions.Expression"), $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Linq.Expressions.ParameterExpression")])], ["TDelegate"]))
    );
});

JSIL.ImplementExternals("System.Linq.Expressions.Expression", function ($) {
    $.Method({ Static: true, Public: true }, "Constant",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), [$.Object], [])),
      function Constant(value) {
          return new System.Linq.Expressions.ConstantExpression(value);
      }
    );

    $.Method({ Static: true, Public: true }, "Constant",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), [$.Object, $jsilcore.TypeRef("System.Type")], [])),
      function Constant(value, type) {
          return System.Linq.Expressions.ConstantExpression.Make(value, type);
      }
    );

    var $TParameterExpressionEnumerable = function () {
        return ($T16 = JSIL.Memoize($jsilcore.System.Collections.Generic.IEnumerable$b1.Of($jsilcore.System.Linq.Expressions.ParameterExpression)))();
    };

    $.Method({ Static: true, Public: true }, "Lambda",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.Expression`1", ["!!0"]), [$jsilcore.TypeRef("System.Linq.Expressions.Expression"), $jsilcore.TypeRef("System.Array", [$jsilcore.TypeRef("System.Linq.Expressions.ParameterExpression")])], ["TDelegate"])),
      function Lambda$b1(TDelegate, body, parameters) {
          var name = null;
          var tailCall = false;
          return new (System.Linq.Expressions.Expression$b1.Of(TDelegate))(body, name, tailCall, $TParameterExpressionEnumerable().$Cast(parameters));
      }
    );

    $.Method({ Static: true, Public: true }, "Parameter",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ParameterExpression"), [$jsilcore.TypeRef("System.Type")], []),
      function Parameter(type) {
          return System.Linq.Expressions.ParameterExpression.Make(type, null, type.IsByRef);
      }
    );

    $.Method({ Static: true, Public: true }, "Parameter",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ParameterExpression"), [$jsilcore.TypeRef("System.Type"), $.String], []),
      function Parameter(type, name) {
          return System.Linq.Expressions.ParameterExpression.Make(type, name, type.IsByRef);
      }
    );
});
JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.Expression"), "System.Linq.Expressions.BinaryExpression", true, [], function ($) {
    var $thisType = $.publicInterface;
});
JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.Expression"), "System.Linq.Expressions.ConstantExpression", true, [], function ($) {
    var $thisType = $.publicInterface;

    $.ExternalMethod({ Static: true, Public: false }, "Make",
      (new JSIL.MethodSignature($.Type, [$.Object, $jsilcore.TypeRef("System.Type")], []))
    );
});

JSIL.ImplementExternals("System.Linq.Expressions.ConstantExpression", function ($) {
    $.Method({ Static: false, Public: false }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Object], [])),
      function _ctor(value) {
          this._value = value;
      }
    );

    $.Method({ Static: true, Public: false }, "Make",
      (new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), [$.Object, $jsilcore.TypeRef("System.Type")], [])),
      function Make(value, type) {
          if (value == null && type == $jsilcore.System.Object.__Type__ || value != null && JSIL.GetType(value) == type) {
              return new System.Linq.Expressions.ConstantExpression(value);
          } else {
              return new System.Linq.Expressions.TypedConstantExpression(value, type);
          }
      }
    );
});
JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.ConstantExpression"), "System.Linq.Expressions.TypedConstantExpression", true, [], function ($) {
    var $thisType = $.publicInterface;
});


JSIL.ImplementExternals("System.Linq.Expressions.TypedConstantExpression", function ($) {
    $.Method({ Static: false, Public: false }, ".ctor",
      (new JSIL.MethodSignature(null, [$.Object, $jsilcore.TypeRef("System.Type")], [])),
      function _ctor(value, type) {
          this._value = value;
          this._type = type;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Type",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
      function get_Type() {
          return this._type;
      }
    );
});
JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.Expression"), "System.Linq.Expressions.ParameterExpression", true, [], function ($) {
    var $thisType = $.publicInterface;
});


JSIL.ImplementExternals("System.Linq.Expressions.ParameterExpression", function ($) {
    $.Method({ Static: true, Public: false }, "Make",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Linq.Expressions.ParameterExpression"), [
          $jsilcore.TypeRef("System.Type"), $.String,
          $.Boolean
      ], []),
      function Make(type, name, isByRef) {
          var experession = new System.Linq.Expressions.ParameterExpression(name);
          experession._type = type;
          experession._isByRef = isByRef;
          return experession;
      }
    );

    $.Method({ Static: false, Public: true }, "get_Type",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Type"), [], []),
      function get_Type() {
          return this._type;
      }
    );

    $.Method({ Static: false, Public: true }, "get_IsByRef",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [], []),
      function get_IsByRef() {
          return this._isByRef;
      }
    );
});
JSIL.ImplementExternals("System.Linq.Expressions.Expression`1", function ($) {
});

JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.Expression"), "System.Linq.Expressions.LambdaExpression", true, [], function ($) {
    var $thisType = $.publicInterface;
});

JSIL.MakeClass($jsilcore.TypeRef("System.Linq.Expressions.LambdaExpression"), "System.Linq.Expressions.Expression`1", true, ["TDelegate"], function ($) {
  var $thisType = $.publicInterface;
});




