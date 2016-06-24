using System;
using Sharper.C.Data;
using static Sharper.C.Data.Unit;

namespace Sharper.C.Control
{
    public static class IOModule
    {
        public static IO<E, A> Handle<E, Ex, A>
          ( this IO<E, Or<Ex, A>> io
          , Func<Ex, IO<E, A>> handler
          )
        =>  io.FlatMap(or => or.Cata(handler, IO<E>.Pure));

        public static IO<E, Unit> WhenJust<E, A>
          ( this Maybe<A> m
          , Func<A, IO<E, Unit>> action
          )
        =>  m.Map(action).ValueOr(IO<E>.Pure(UNIT));

        public static IO<E, Or<A, B>> Sequence<E, A, B>(this Or<A, IO<E, B>> or)
        =>  or.Cata
              ( a => IO<E>.Pure(Or.Left<A, B>(a))
              , b => b.Map(Or.Right<A, B>)
              );

        public static IO<E, Or<A, C>> Traverse<E, A, B, C>
          ( this Or<A, B> or
          , Func<B, IO<E, C>> f
          )
        =>  or.Map(f).Sequence();

        public static IO<E, Or<A, B>>
        SequenceLeft<E, A, B>(this Or<IO<E, A>, B> or)
        =>  or.Swap.Sequence().Map(x => x.Swap);

        public static IO<E, Or<C, B>> TraverseLeft<E, A, B, C>
          ( this Or<A, B> or
          , Func<A, IO<E, C>> f
          )
        =>  or.MapLeft(f).SequenceLeft();

        public static IO<E, Maybe<A>> Sequence<E, A>(this Maybe<IO<E, A>> ma)
        =>  ma.Cata
              ( () => IO<E>.Pure(Maybe.Nothing<A>())
              , a => a.Map(Maybe.Just)
              );

        public static IO<E, Maybe<B>> Traverse<E, A, B>
          ( this Maybe<A> ma
          , Func<A, IO<E, B>> f
          )
        =>  ma.Map(f).Sequence();

        public static IO<E, Maybe<B>> MapT<E, A, B>
          ( this IO<E, Maybe<A>> ioma
          , Func<A, B> f
          )
        =>  ioma.Map(ma => ma.Map(f));

        public static IO<E, Maybe<B>> FlatMapT<E, A, B>
          ( this IO<E, Maybe<A>> ioma
          , Func<A, IO<E, Maybe<B>>> f
          )
        =>  ioma.FlatMap(ma => ma.Traverse(f).Map(mma => mma.Join()));

        public static IO<E, Or<X, B>> MapT<E, X, A, B>
          ( this IO<E, Or<X, A>> ioma
          , Func<A, B> f
          )
        =>  ioma.Map(ma => ma.Map(f));

        public static IO<E, Or<X, B>> FlatMapT<E, X, A, B>
          ( this IO<E, Or<X, A>> ioma
          , Func<A, IO<E, Or<X, B>>> f
          )
        =>  ioma.FlatMap(ma => ma.Traverse(f).Map(mma => mma.Join()));

        public static IO<E, Or<Y, A>> MapLeftT<E, X, Y, A>
          ( this IO<E, Or<X, A>> iomx
          , Func<X, Y> f
          )
        =>  iomx.Map(or => or.Swap).MapT(f).Map(or => or.Swap);

        public static IO<E, Or<Y, A>> FlatMapLeftT<E, X, Y, A>
          ( this IO<E, Or<X, A>> iomx
          , Func<X, IO<E, Or<Y, A>>> f
          )
        =>  iomx.FlatMap(or => or.TraverseLeft(f).Map(x => x.JoinLeft()));

        public static IO<E, Maybe<A>>
        JoinT<E, A>(this IO<E, Maybe<IO<E, Maybe<A>>>> io)
        =>  io.FlatMapT(x => x);

        public static IO<E, Or<X, A>>
        JoinT<E, X, A>(this IO<E, Or<X, IO<E, Or<X, A>>>> io)
        =>  io.FlatMapT(x => x);

        public static IO<E, Or<X, A>>
        JoinLeftT<E, X, A>(this IO<E, Or<IO<E, Or<X, A>>, A>> io)
        =>  io.FlatMapLeftT(x => x);

        public static IO<E, A> OrT<E, A>
          ( this IO<E, Maybe<A>> io
          , IO<E, A> a
          )
        =>  io.FlatMap(ma => ma.Cata(() => a, IO<E>.Pure));

        private static Maybe<A> Join<A>(this Maybe<Maybe<A>> m)
        =>  m.ValueOr(Maybe.Nothing<A>);

        private static Or<X ,A> Join<X, A>(this Or<X ,Or<X ,A>> m)
        =>  m.RightValueOr(Or.Left<X, A>);
    }
}
