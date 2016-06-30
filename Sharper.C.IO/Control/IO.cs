using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Sharper.C.Data;
using static Sharper.C.Data.Unit;

namespace Sharper.C.Control
{
    public static class IO<E>
    {
        public static IO<E, A> Pure<A>(A a)
        =>  new IO<E, A>(() => Task.FromResult(a));

        public static IO<E, A> Defer<A>(Func<A> a)
        =>  new IO<E, A>(() => Task.Run(a));

        public static IO<E, Unit> Defer(Action a)
        =>  new IO<E, Unit>(() => Task.Run(() => { a(); return UNIT; }));

        public static IO<E, A> Mk<A>(Func<Task<A>> a)
        =>  new IO<E, A>(a);

        public static IO<E, Unit> Mk(Func<Task> a)
        =>  new IO<E, Unit>
              ( async () =>
                {   await a();
                    return UNIT;
                }
              );

        public static IO<E, A> Mk<A>(Func<IOAwaitToken, Task<A>> a)
        =>  new IO<E, A>(() => a(IOAwaitToken.Instance));

        public static IO<E, Unit> Mk(Func<IOAwaitToken, Task> a)
        =>  new IO<E, Unit>
              ( async () =>
                {   await a((IOAwaitToken.Instance));
                    return UNIT;
                }
              );

        public static IO<E, A> Join<E, A>(this IO<E, IO<E, A>> x)
        =>  IO<E>.Mk
              ( async tok =>
                {   var ioa = await x.Awaitable(tok);
                    return await ioa.Awaitable(tok);
                }
              );
    }

    public struct IO<E, A>
    {
        internal IO(Func<Task<A>> task)
        {   this.task = task;
        }

        private readonly Func<Task<A>> task;

        public IO<E, B> Map<B>(Func<A, B> f)
        {   var t = task;
            return new IO<E, B>(async () => f(await t()));
        }

        public IO<E, B> FlatMap<B>(Func<A, IO<E, B>> f)
        {   var self = this;
            return
                new IO<E, B>
                  ( async () => await f(await self.Awaitable()).task()
                  );
        }

        public IO<E, A> FlatMapForEffect<B>(Func<A, IO<E, B>> f)
        =>  FlatMap(a => f(a).Map(_ => a));

        public IO<E, B> Select<B>(Func<A, B> f)
        =>  Map(f);

        public IO<E, C> SelectMany<B, C>(Func<A, IO<E, B>> f, Func<A, B, C> g)
        {   var self = this;
            return
                new IO<E, C>
                  ( async () =>
                    {   var a = await self.Awaitable();
                        var b = await f(a).Awaitable();
                        return g(a, b);
                    }
                  );
        }

        public Task<A> UnsafeUntrackedTask
        =>  task();

        public IO<E, Or<Ex, A>> Recover<Ex>()
          where Ex : Exception
        {   var self = this;
            return
                new IO<E, Or<Ex, A>>
                  ( async () =>
                    {   try
                        {   return Or.Right<Ex, A>(await self.task());
                        }
                        catch (Ex e)
                        {   return Or.Left<Ex, A>(e);
                        }
                    }
                  );
        }

        public IO<E, A> Catch<Ex>(Func<Ex, IO<E, A>> handler)
          where Ex : Exception
        =>  Recover<Ex>().FlatMap(x => x.Cata(handler, IO<E>.Pure<A>));

        public IO<F, A> Interleave<F>()
          where F : E
        =>  UnsafeForceInterleave<F>();

        public IO<F, A> UnsafeForceInterleave<F>()
        =>  new IO<F, A>(task);

        public ConfiguredTaskAwaitable<A> Awaitable(IOAwaitToken token)
        {   Trace.Assert(token != null);
            return Awaitable();
        }

        private ConfiguredTaskAwaitable<A> Awaitable()
        =>  task().ConfigureAwait(false);
    }
}
