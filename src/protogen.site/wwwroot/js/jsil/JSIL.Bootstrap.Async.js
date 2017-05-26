/* It is auto-generated file. Do not modify it. */
"use strict";

if (typeof (JSIL) === "undefined")
  throw new Error("JSIL.Core is required");

JSIL.DeclareNamespace("System.Runtime.CompilerServices");
JSIL.DeclareNamespace("System.Threading");
JSIL.DeclareNamespace("System.Threading.Tasks");

JSIL.ImplementExternals("System.Runtime.CompilerServices.AsyncVoidMethodBuilder", function ($) {
  $.Method({ Static: false, Public: false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
    }
  );

  $.Method({ Static: true, Public: true }, "Create",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.AsyncVoidMethodBuilder"), [], [])),
    function Create() {
      return new $jsilcore.System.Runtime.CompilerServices.AsyncVoidMethodBuilder();
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"]),
    function AwaitOnCompleted(TStateMachine, stateMachine) {
      stateMachine.get().MoveNext();
    }
  );

  $.Method({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [], []),
    function SetResult() {
    }
  );

  $.Method({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], []),
    function SetException(exception) {
      JSIL.Host.warning(exception);
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.ValueType"),
  Name: "System.Runtime.CompilerServices.AsyncVoidMethodBuilder",
  IsPublic: true,
  IsReferenceType: false,
  MaximumConstructorArguments: 1,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: false }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Threading.SynchronizationContext")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: true, Public: true }, "Create",
    new JSIL.MethodSignature($.Type, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetStateMachine",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Runtime.CompilerServices.IAsyncStateMachine")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"])
  );
});
JSIL.ImplementExternals("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", function ($) {
  var $TaskCompletionSourceOfObject = function () {
    return ($TaskCompletionSourceOfObject = JSIL.Memoize($jsilcore.System.Threading.Tasks.TaskCompletionSource$b1.Of($jsilcore.System.Object)))();
  };

  var $TrySetExceptionSignature = function () {
    return ($TrySetExceptionSignature = JSIL.Memoize(new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.Exception")])))();
  };

  $.Method({ Static: false, Public: false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
    }
  );

  $.Method({ Static: true, Public: true }, "Create",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.AsyncTaskMethodBuilder"), [], [])),
    function Create() {
      return new $jsilcore.System.Runtime.CompilerServices.AsyncTaskMethodBuilder();
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"]),
    function AwaitOnCompleted(TStateMachine, stateMachine) {
      stateMachine.get().MoveNext();
    }
  );

  $.Method({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [], []),
    function SetResult() {
      return $TaskCompletionSourceOfObject().prototype.TrySetResult.call(this.get_TaskSource(), null);
    }
  );

  $.Method({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], []),
    function SetException(exception) {
      JSIL.Host.warning(exception);
      $TrySetExceptionSignature().Call($TaskCompletionSourceOfObject().prototype, "TrySetException", null, this.get_TaskSource(), exception);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Task",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [], []),
    function get_Task() {
      return $TaskCompletionSourceOfObject().prototype.get_Task.call(this.get_TaskSource());
    }
  );

  $.RawMethod(false, "get_TaskSource",
    function get_TaskSource() {
      if (!this._taskSource) {
        this._taskSource = new ($TaskCompletionSourceOfObject())();
      }
      return this._taskSource;
    }
  );
});

JSIL.ImplementExternals("System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1", function ($) {
  var $TrySetExceptionSignature = function () {
    return ($TrySetExceptionSignature = JSIL.Memoize(new JSIL.MethodSignature($jsilcore.TypeRef("System.Boolean"), [$jsilcore.TypeRef("System.Exception")])))();
  };

  $.Method({ Static: false, Public: false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
    }
  );

  $.Method({ Static: true, Public: true }, "Create",
    (new JSIL.MethodSignature($.Type, [], [])),
    function Create() {
      return new ($jsilcore.System.Runtime.CompilerServices.AsyncTaskMethodBuilder$b1.Of(this.TResult))();
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"]),
    function AwaitOnCompleted(TAwaiter, TStateMachine, awaiter, stateMachine) {
      stateMachine = stateMachine.get();

      var completedInterfaceMethod = $jsilcore.System.Runtime.CompilerServices.INotifyCompletion.OnCompleted;
      completedInterfaceMethod.Call(awaiter.get(), null, $jsilcore.System.Action.New(stateMachine, stateMachine.MoveNext));
    }
  );

  $.Method({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"]),
    function AwaitOnCompleted(TStateMachine, stateMachine) {
      stateMachine.get().MoveNext();
    }
  );

  $.Method({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")], []),
    function SetResult(result) {
      var taskCompletionSource = $jsilcore.System.Threading.Tasks.TaskCompletionSource$b1.Of(this.TResult);
      return taskCompletionSource.prototype.TrySetResult.call(this.get_TaskSource(), result);
    }
  );

  $.Method({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], []),
    function SetException(exception) {
      JSIL.Host.warning(exception);
      var taskCompletionSource = $jsilcore.System.Threading.Tasks.TaskCompletionSource$b1.Of(this.TResult);
      $TrySetExceptionSignature().Call(taskCompletionSource.prototype, "TrySetException", null, this.get_TaskSource(), exception);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Task",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")]), [], []),
    function get_Task() {
      var taskCompletionSource = $jsilcore.System.Threading.Tasks.TaskCompletionSource$b1.Of(this.TResult);
      return taskCompletionSource.prototype.get_Task.call(this.get_TaskSource());
    }
  );

  $.RawMethod(false, "get_TaskSource",
    function get_TaskSource() {
      if (!this._taskSource) {
        this._taskSource = new ($jsilcore.System.Threading.Tasks.TaskCompletionSource$b1.Of(this.TResult))();
      }
      return this._taskSource;
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.ValueType"),
  Name: "System.Runtime.CompilerServices.AsyncTaskMethodBuilder",
  IsPublic: true,
  IsReferenceType: false,
  MaximumConstructorArguments: 0,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: true, Public: true }, "Create",
    new JSIL.MethodSignature($.Type, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Task",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetStateMachine",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Runtime.CompilerServices.IAsyncStateMachine")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"])
  );

  $.Property({ Static: false, Public: true }, "Task", $jsilcore.TypeRef("System.Threading.Tasks.Task"));
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.ValueType"),
  Name: "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1",
  IsPublic: true,
  IsReferenceType: false,
  GenericParameters: ["TResult"],
  MaximumConstructorArguments: 0,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, "AwaitOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: false, Public: true }, "AwaitUnsafeOnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"]), $jsilcore.TypeRef("JSIL.Reference", ["!!1"])], ["TAwaiter", "TStateMachine"])
  );

  $.ExternalMethod({ Static: true, Public: true }, "Create",
    new JSIL.MethodSignature($.Type, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Task",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")]), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetException",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Exception")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetResult",
    new JSIL.MethodSignature(null, [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "SetStateMachine",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Runtime.CompilerServices.IAsyncStateMachine")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "Start",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("JSIL.Reference", ["!!0"])], ["TStateMachine"])
  );

  $.ExternalMethod({ Static: false, Public: false, Virtual: true }, "System.Runtime.CompilerServices.IAsyncMethodBuilder.PreBoxInitialization",
    new JSIL.MethodSignature(null, [], [])
  );

  $.Property({ Static: false, Public: true }, "Task", $jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1")]));
});
JSIL.ImplementExternals("System.Threading.Tasks.Task", function ($) {
  var $AggregateExceptionConstructorSignature = function () {
    return ($AggregateExceptionConstructorSignature = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.AggregateException"), [$jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.Exception")])))();
  };

  // TODO: Find solution to remove closure
  var createTaskCommon = function (self) {
    self.status = System.Threading.Tasks.TaskStatus.Created;
    self.action = null;
    self.exception = null;

    self.ContinueExecution = function () {
      // TODO: init continuationActions with null on ctor
      if (this.continuationActions !== undefined) {
        for (var i in this.continuationActions) {
          this.continuationActions[i](this);
        }
      }
    }

    self.SetComplete = function () {
      this.status = System.Threading.Tasks.TaskStatus.RanToCompletion;
      this.ContinueExecution();
    }

    self.SetCancel = function () {
      this.status = System.Threading.Tasks.TaskStatus.Canceled;
      this.ContinueExecution();
    }

    self.SetException = function (exception) {
      this.status = System.Threading.Tasks.TaskStatus.Faulted;
      this.exception = $AggregateExceptionConstructorSignature().Construct("One or more errors occured.", exception);
      this.ContinueExecution();
    }

    self.RunTask = function () {
      if (this.action !== null) {
        try {
          this.action();
          this.SetComplete();
        } catch (e) {
          this.SetException(e);
        }
      }
    }
  }

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], [])),
    function _ctor(action) {
      createTaskCommon(this);
      this.action = action;
    }
  );

  $.Method({ Static: false, Public: false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
      createTaskCommon(this);
    }
  );

  $.Method({ Static: false, Public: true }, "get_Status",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskStatus"), [], [])),
    function get_Status() {
      return this.status;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Exception",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.AggregateException"), [], [])),
    function get_Exception() {
      return this.exception;
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsCompleted",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function get_IsCompleted() {
      return (this.status == System.Threading.Tasks.TaskStatus.RanToCompletion
            || this.status == System.Threading.Tasks.TaskStatus.Canceled
            || this.status == System.Threading.Tasks.TaskStatus.Faulted);
    }
  );

  $.Method({ Static: false, Public: true }, "get_IsCanceled",
      (new JSIL.MethodSignature($.Boolean, [], [])),
      function get_IsCanceled() {
          return (this.status == System.Threading.Tasks.TaskStatus.Canceled);
      }
  );

  $.Method({ Static: false, Public: true }, "get_IsFaulted",
      (new JSIL.MethodSignature($.Boolean, [], [])),
      function get_IsFaulted() {
          return (this.status == System.Threading.Tasks.TaskStatus.Faulted);
      }
  );

  $.Method({ Static: false, Public: true }, "ContinueWith",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action`1", [$jsilcore.TypeRef("System.Threading.Tasks.Task")])], [])),
    function ContinueWith(continuationAction) {
      if (this.get_IsCompleted()) {
        continuationAction(this);
        return;
      }

      if (this.continuationActions === undefined) {
        this.continuationActions = [];
      }

      this.continuationActions.push(continuationAction);
    }
  );

  $.Method({ Static: true, Public: true }, "get_Factory",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskFactory"), [], [])),
    function get_Factory() {
      // TODO: Think about caching factory
      return new System.Threading.Tasks.TaskFactory();
    }
  );

  $.Method({ Static: false, Public: true }, "GetAwaiter",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.TaskAwaiter"), [], [])),
    function GetAwaiter() {
      return new $jsilcore.System.Runtime.CompilerServices.TaskAwaiter(this);
    }
  );

  $.Method({ Static: true, Public: true }, "Delay",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$.Int32], [])),
    function Delay(dueTime) {
      var tcs = new (System.Threading.Tasks.TaskCompletionSource$b1.Of(System.Boolean))();
      setTimeout(function () { tcs.TrySetResult(true); }, dueTime);
      return tcs.Task;
    }
  );

  $.Method({ Static: true, Public: true }, "FromResult",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", ["!!0"]), ["!!0"], ["TResult"]),
      function(TResult, result) {
          var task = new ($jsilcore.System.Threading.Tasks.Task$b1.Of(TResult));
          task.result = result;
          task.SetComplete();
          return task;
      }
  );
});

JSIL.ImplementExternals("System.Threading.Tasks.Task`1", function ($) {
  var $AggregateExceptionConstructorSignature = function () {
    return ($AggregateExceptionConstructorSignature = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.AggregateException"), [$jsilcore.TypeRef("System.String"), $jsilcore.TypeRef("System.Exception")])))();
  };
  var $TaskCanceledExceptionExceptionConstructorSignature = function () {
    return ($TaskCanceledExceptionExceptionConstructorSignature = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskCanceledException"), [])))();
  };

  var createTaskCommon = function (self) {
    self.$function = null;
    self.RunTask = function () {
      if (this.$function !== null) {
        try {
          this.result = this.$function();
          this.SetComplete();
        } catch (e) {
          this.SetException(e);
        }
      }
    }
  }

  $.Method({ Static: false, Public: true }, "ContinueWith",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action`1", [$.Type])], []),
    function ContinueWith(continuationAction) {
      if (this.continuationActions === undefined) {
        this.continuationActions = [];
      }

      this.continuationActions.push(continuationAction);
    }
  );

  $.Method({ Static: false, Public: false }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
      System.Threading.Tasks.Task.prototype._ctor.call(this);
      createTaskCommon(this);
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Func`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")])], [])),
    function _ctor($function) {
      System.Threading.Tasks.Task.prototype._ctor.call(this);
      createTaskCommon(this);
      this.$function = $function;
    }
  );

  $.Method({ Static: false, Public: true }, "get_Result",
    (new JSIL.MethodSignature(new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1"), [], [])),
    function get_Result() {
      var taskException = this.get_Exception();
      if (taskException !== null) {
        throw taskException;
      }
      if (this.get_IsCanceled()) {
          throw $AggregateExceptionConstructorSignature().Construct("One or more errors occured.", $TaskCanceledExceptionExceptionConstructorSignature().Construct());
      }
      return this.result;
    }
  );

  $.Method({ Static: true, Public: true }, "get_Factory",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskFactory`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")]), [], [])),
    function get_Factory() {
      return new (System.Threading.Tasks.TaskFactory$b1.Of(System.Threading.Tasks.Task$b1.TResult.get(this)))();
    }
  );

  $.Method({ Static: false, Public: true }, "GetAwaiter",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.TaskAwaiter`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")]), [], [])),
    function GetAwaiter() {
      return new ($jsilcore.System.Runtime.CompilerServices.TaskAwaiter$b1.Of(this.TResult))(this);
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Object"),
  Name: "System.Threading.Tasks.Task",
  IsPublic: true,
  IsReferenceType: true,
  MaximumConstructorArguments: 8,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: false }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "ContinueWith",
    new JSIL.MethodSignature($.Type, [$jsilcore.TypeRef("System.Action`1", [$.Type])], [])
  );

  $.ExternalMethod({ Static: true, Public: true }, "Delay",
    new JSIL.MethodSignature($.Type, [$.Int32], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Exception",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.AggregateException"), [], [])
  );

  $.ExternalMethod({ Static: true, Public: true }, "get_Factory",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskFactory"), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true, Virtual: true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Status",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskStatus"), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "GetAwaiter",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.TaskAwaiter"), [], [])
  );

  $.ExternalMethod({ Static: true, Public: true }, "FromResult",
      new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", ["!!0"]), ["!!0"], ["TResult"])
  );

  $.Property({ Static: false, Public: true }, "Exception", $jsilcore.TypeRef("System.AggregateException"));

  $.Property({ Static: false, Public: true, Virtual: true }, "IsCompleted", $.Boolean);
  $.Property({ Static: false, Public: true, Virtual: true }, "IsFaulted", $.Boolean);
  $.Property({ Static: false, Public: true, Virtual: true }, "IsCanceled", $.Boolean);

  $.Property({ Static: true, Public: true }, "Factory", $jsilcore.TypeRef("System.Threading.Tasks.TaskFactory"));

  $.Property({ Static: false, Public: true }, "Status", $jsilcore.TypeRef("System.Threading.Tasks.TaskStatus"));
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Threading.Tasks.Task"),
  Name: "System.Threading.Tasks.Task`1",
  IsPublic: true,
  IsReferenceType: true,
  GenericParameters: ["TResult"],
  MaximumConstructorArguments: 8,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: false }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Func`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")])], [])
  );

  $.ExternalMethod({ Static: true, Public: true }, "get_Factory",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskFactory`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")]), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Result",
    new JSIL.MethodSignature(new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1"), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "GetAwaiter",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Runtime.CompilerServices.TaskAwaiter`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")]), [], [])
  );

  $.Property({ Static: false, Public: true }, "Result", new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1"));

  $.GenericProperty({ Static: true, Public: true }, "Factory", $jsilcore.TypeRef("System.Threading.Tasks.TaskFactory`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.Task`1")]));
});
JSIL.ImplementExternals("System.Threading.Tasks.TaskCompletionSource`1", function ($) {
  $.Method({ Static: false, Public: true }, ".ctor",
    (new JSIL.MethodSignature(null, [], [])),
    function _ctor() {
      this.task = new (System.Threading.Tasks.Task$b1.Of(System.Threading.Tasks.TaskCompletionSource$b1.TResult.get(this)))();
    }
  );

  $.Method({ Static: false, Public: true }, "get_Task",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskCompletionSource`1")]), [], [])),
    function get_Task() {
      return this.task;
    }
  );

  $.Method({ Static: false, Public: true }, "TrySetResult",
    (new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskCompletionSource`1")], [])),
    function TrySetResult(result) {
      if (this.task.IsCompleted)
        return false;

      this.task.result = result;
      this.task.SetComplete();
      return true;
    }
  );

  $.Method({ Static: false, Public: true }, "TrySetCanceled",
    (new JSIL.MethodSignature($.Boolean, [], [])),
    function TrySetCanceled() {
      if (this.task.IsCompleted)
        return false;

      this.task.SetCancel();
      return true;
    }
  );

  $.Method({ Static: false, Public: true }, "TrySetException",
    (new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Exception")], [])),
    function TrySetException(exception) {
      if (this.task.IsCompleted)
        return false;

      this.task.SetException(exception);
      return true;
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Object"),
  Name: "System.Threading.Tasks.TaskCompletionSource`1",
  IsPublic: true,
  IsReferenceType: true,
  GenericParameters: ["TResult"],
  MaximumConstructorArguments: 2,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_Task",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskCompletionSource`1")]), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "TrySetCanceled",
    new JSIL.MethodSignature($.Boolean, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "TrySetException",
    new JSIL.MethodSignature($.Boolean, [$jsilcore.TypeRef("System.Exception")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "TrySetResult",
    new JSIL.MethodSignature($.Boolean, [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskCompletionSource`1")], [])
  );

  $.Property({ Static: false, Public: true }, "Task", $jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskCompletionSource`1")]));
});

JSIL.ImplementExternals("System.Threading.Tasks.TaskFactory", function ($) {
  $.Method({ Static: false, Public: true }, "StartNew",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action")], [])),
    function StartNew(action) {
      var task = new System.Threading.Tasks.Task(action);
      task.RunTask();
      return task;
    }
  );

  $.Method({ Static: false, Public: true }, "StartNew",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", ["!!0"]), [$jsilcore.TypeRef("System.Func`1", ["!!0"])], ["TResult"])),
    function StartNew$b1(TResult, $function) {
      var task = new (System.Threading.Tasks.Task$b1.Of(TResult.__PublicInterface__))();
      task.$function = $function;
      task.RunTask();
      return task;
    }
  );
});

JSIL.ImplementExternals("System.Threading.Tasks.TaskFactory`1", function ($) {
  $.Method({ Static: false, Public: true }, "StartNew",
    (new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskFactory`1")]), [$jsilcore.TypeRef("System.Func`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskFactory`1")])], [])),
    function StartNew($function) {
      var task = new (System.Threading.Tasks.Task$b1.Of(System.Threading.Tasks.TaskFactory$b1.TResult.get(this)))($function);
      task.RunTask();
      return task;
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Object"),
  Name: "System.Threading.Tasks.TaskFactory",
  IsPublic: true,
  IsReferenceType: true,
  MaximumConstructorArguments: 4,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "StartNew",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "StartNew",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", ["!!0"]), [$jsilcore.TypeRef("System.Func`1", ["!!0"])], ["TResult"])
  )
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Object"),
  Name: "System.Threading.Tasks.TaskFactory`1",
  IsPublic: true,
  IsReferenceType: true,
  GenericParameters: ["TResult"],
  MaximumConstructorArguments: 4,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "StartNew",
    new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskFactory`1")]), [$jsilcore.TypeRef("System.Func`1", [new JSIL.GenericParameter("TResult", "System.Threading.Tasks.TaskFactory`1")])], [])
  );
});
JSIL.ImplementExternals("System.Runtime.CompilerServices.TaskAwaiter", function ($) {
  var $TaskCanceledExceptionExceptionConstructorSignature = function () {
    return ($TaskCanceledExceptionExceptionConstructorSignature = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskCanceledException"), [])))();
  };

  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Threading.Tasks.Task")], []),
      function TaskAwaiter__ctor(task) {
        // TODO: Check MemberwiseClone here. We don't define _task as field. Is it work?
        this._task = task;
      }
  );

  $.Method({ Static: false, Public: true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], []),
    function MyTaskAwaiter_get_IsCompleted() {
      return this._task.get_IsCompleted();
    }
  );

  $.Method({ Static: false, Public: true }, "GetResult",
    new JSIL.MethodSignature(null, [], []),
    function GetResult() {
      if (!this._task.get_IsCompleted()) {
        throw new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Exception"), [$jsilcore.TypeRef("System.String")]).Construct("TaskNotCompleted");
      }
      if (this._task.get_IsCanceled()) {
        throw $TaskCanceledExceptionExceptionConstructorSignature().Construct();
      }
      var taskException = this._task.get_Exception();
      if (taskException !== null) {
        throw taskException.get_InnerException();
      }
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "OnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], []),
    function MyTaskAwaiter_OnCompleted(continuation) {
      var continueSignature = new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action`1", [$jsilcore.TypeRef("System.Threading.Tasks.Task")])], []);
      continueSignature.CallVirtual("ContinueWith", null, this._task, function (task) { continuation() });
    }
  );
});

JSIL.ImplementExternals("System.Runtime.CompilerServices.TaskAwaiter`1", function ($) {
  var $TaskCanceledExceptionExceptionConstructorSignature = function () {
    return ($TaskCanceledExceptionExceptionConstructorSignature = JSIL.Memoize(new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Threading.Tasks.TaskCanceledException"), [])))();
  };

  $.Method({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.TaskAwaiter`1")])], []),
      function TaskAwaiter__ctor(task) {
        this._task = task;
      }
  );

  $.Method({ Static: false, Public: true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], []),
    function MyTaskAwaiter_get_IsCompleted() {
      return this._task.get_IsCompleted();
    }
  );

  $.Method({ Static: false, Public: true }, "GetResult",
    new JSIL.MethodSignature(new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.TaskAwaiter`1"), [], []),
    function GetResult() {
      if (!this._task.get_IsCompleted()) {
        throw new JSIL.ConstructorSignature($jsilcore.TypeRef("System.Exception"), [$jsilcore.TypeRef("System.String")]).Construct("TaskNotCompleted");
      }
      if (this._task.get_IsCanceled()) {
          throw $TaskCanceledExceptionExceptionConstructorSignature().Construct();
      }
      var taskException = this._task.get_Exception();
      if (taskException !== null) {
        throw taskException.get_InnerException();
      }
      return this._task.get_Result();
    }
  );

  $.Method({ Static: false, Public: true, Virtual: true }, "OnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], []),
    function MyTaskAwaiter_OnCompleted(continuation) {
      var continueSignature = new JSIL.MethodSignature($jsilcore.TypeRef("System.Threading.Tasks.Task"), [$jsilcore.TypeRef("System.Action`1", [$jsilcore.TypeRef("System.Threading.Tasks.Task")])], []);
      continueSignature.CallVirtual("ContinueWith", null, this._task, function (task) { continuation() });
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.ValueType"),
  Name: "System.Runtime.CompilerServices.TaskAwaiter",
  IsPublic: true,
  IsReferenceType: false,
  MaximumConstructorArguments: 1,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: false }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Threading.Tasks.Task")], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "GetResult",
    new JSIL.MethodSignature(null, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true, Virtual: true }, "OnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], [])
  );

  $.Property({ Static: false, Public: true }, "IsCompleted", $.Boolean);

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("System.Runtime.CompilerServices.INotifyCompletion")
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.ValueType"),
  Name: "System.Runtime.CompilerServices.TaskAwaiter`1",
  IsPublic: true,
  IsReferenceType: false,
  GenericParameters: ["TResult"],
  MaximumConstructorArguments: 1,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: false }, ".ctor",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Threading.Tasks.Task`1", [new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.TaskAwaiter`1")])], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "get_IsCompleted",
    new JSIL.MethodSignature($.Boolean, [], [])
  );

  $.ExternalMethod({ Static: false, Public: true }, "GetResult",
    new JSIL.MethodSignature(new JSIL.GenericParameter("TResult", "System.Runtime.CompilerServices.TaskAwaiter`1"), [], [])
  );

  $.ExternalMethod({ Static: false, Public: true, Virtual: true }, "OnCompleted",
    new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], [])
  );

  $.Property({ Static: false, Public: true }, "IsCompleted", $.Boolean);

  $.ImplementInterfaces(
    /* 0 */ $jsilcore.TypeRef("System.Runtime.CompilerServices.INotifyCompletion")
  );
});
JSIL.MakeInterface(
  "System.Runtime.CompilerServices.IAsyncStateMachine", true, [], function ($) {
    $.Method({}, "MoveNext", new JSIL.MethodSignature(null, [], []));
    $.Method({}, "SetStateMachine", new JSIL.MethodSignature(null, [$.Type], []));
  }, []);
JSIL.MakeEnum(
"System.Threading.Tasks.TaskStatus", true, {
  Created: 0,
  WaitingForActivation: 1,
  WaitingToRun: 2,
  Running: 3,
  WaitingForChildrenToComplete: 4,
  RanToCompletion: 5,
  Canceled: 6,
  Faulted: 7
}, false
);
JSIL.MakeInterface(
  "System.Runtime.CompilerServices.INotifyCompletion", true, [], function ($) {
    $.Method({}, "OnCompleted", new JSIL.MethodSignature(null, [$jsilcore.TypeRef("System.Action")], []));
  }, []);
JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Exception"),
  Name: "System.AggregateException",
  IsPublic: true,
  IsReferenceType: true,
  MaximumConstructorArguments: 2,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [$.String, $jsilcore.TypeRef("System.Exception")], [])
  );
});
JSIL.MakeType({
    BaseType: $jsilcore.TypeRef("System.OperationCanceledException"),
    Name: "System.Threading.Tasks.TaskCanceledException",
    IsPublic: true,
    IsReferenceType: true,
    MaximumConstructorArguments: 2,
}, function ($interfaceBuilder) {
    var $ = $interfaceBuilder;

    $.ExternalMethod({ Static: false, Public: true }, ".ctor",
      new JSIL.MethodSignature(null, [], [])
    );
});
JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.SystemException"),
  Name: "System.OperationCanceledException",
  IsPublic: true,
  IsReferenceType: true,
  MaximumConstructorArguments: 2,
}, function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    new JSIL.MethodSignature(null, [], [])
  );
});
JSIL.ImplementExternals("System.Threading.ManualResetEventSlim", function ($interfaceBuilder) {
  var $ = $interfaceBuilder;

  $.Method({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Void,
    function _ctor() {
      // FIXME
    }
  );

  $.Method({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Action($.Boolean),
    function _ctor(initialState) {
      // FIXME
    }
  );

  $.Method({ Static: false, Public: false }, "Set",
    JSIL.MethodSignature.Action($.Boolean),
    function () {
      // FIXME
    }
  );

  $.Method({ Static: false, Public: true }, "Wait",
    JSIL.MethodSignature.Void,
    function () {
      // FIXME
    }
  );

  $.Method({ Static: false, Public: true }, "Wait",
    new JSIL.MethodSignature($.Boolean, [$.Int32]),
    function (duration) {
      // FIXME
    }
  );
});

JSIL.MakeType({
  BaseType: $jsilcore.TypeRef("System.Object"),
  Name: "System.Threading.ManualResetEventSlim",
  IsPublic: true,
  IsReferenceType: true,
  MaximumConstructorArguments: 2,
}, function ($) {
  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Void
  );

  $.ExternalMethod({ Static: false, Public: true }, ".ctor",
    JSIL.MethodSignature.Action($.Boolean)
  );

  $.ExternalMethod({ Static: false, Public: false }, "Set",
    JSIL.MethodSignature.Action($.Boolean)
  );

  $.ExternalMethod({ Static: false, Public: true }, "Wait",
    JSIL.MethodSignature.Void
  );

  $.ExternalMethod({ Static: false, Public: true }, "Wait",
    new JSIL.MethodSignature($.Boolean, [$.Int32])
  );
}
);
