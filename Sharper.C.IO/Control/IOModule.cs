using System;
using Sharper.C.Data;
using static Sharper.C.Data.Unit;

namespace Sharper.C.Control
{
    public static class IOModule
    {
        public static IO<E, Unit> WhenJust<E, A>
          ( this Maybe<A> m
          , Func<A, IO<E, Unit>> action
          )
        => m.Map(action).ValueOr(IO<E>.Pure(UNIT));

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

        private static Maybe<A> Join<A>(this Maybe<Maybe<A>> m)
        =>  m.ValueOr(Maybe.Nothing<A>);

        private static Or<X ,A> Join<X, A>(this Or<X ,Or<X ,A>> m)
        =>  m.RightValueOr(Or.Left<X, A>);
    }
}
