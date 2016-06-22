using System;
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
                  ( async () => await f(await self.Awaitable).task()
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
                    {   var a = await self.Awaitable;
                        var b = await f(a).Awaitable;
                        return g(a, b);
                    }
                  );
        }

        public Task<A> UnsafeTask
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

        public IO<E0, A> Interleave<E0>()
          where E0 : E
        =>  new IO<E0, A>(task);

        public IO<E0, A> As<E0>()
          where E0 : E
        =>  Interleave<E0>();

        private ConfiguredTaskAwaitable<A> Awaitable
        =>  task().ConfigureAwait(false);

        public static ConfiguredTaskAwaitable<A> operator ~(IO<E, A> io)
        =>  io.Awaitable;
    }
}
