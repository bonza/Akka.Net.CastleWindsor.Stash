namespace Akka.Stash
{
    using System;

    using Akka.Actor;
    using Akka.DI.CastleWindsor;
    using Akka.DI.Core;

    using Castle.MicroKernel.Registration;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor;

    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("mySystem");

            var container = new WindsorContainer();
            container.Install(new CommonWindsorInstaller());

            // ReSharper disable once ObjectCreationAsStatement
            new WindsorDependencyResolver(container, actorSystem);

            var rootActor = actorSystem.ActorOf(actorSystem.DI().Props<RootActor>(), typeof(RootActor).Name);

            rootActor.Tell(new CreateChildMessage());

            Console.ReadLine();
        }
    }

    internal class CreateChildMessage
    {
    }

    internal class CommonWindsorInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<RootActor>().Named("RootActor").LifestyleTransient());
            container.Register(Component.For<ChildActor>().Named("ChildActor").LifestyleTransient());
            container.Register(Component.For<IChildActor>().ImplementedBy<ChildActor>().Named("IChildActor").LifestyleTransient());
            container.Register(Component.For<IWithUnboundedStash>().ImplementedBy<ChildActor>().Named("IWithUnboundedStash").LifestyleTransient());

        }
    }

    internal class RootActor : ReceiveActor
    {
        public RootActor()
        {
            Receive<CreateChildMessage>(
                m =>
                    {
                        var child = ActorHelper.CreateActor(Context, typeof(IChildActor), "child");
                    });
        }
    }

    public interface IChildActor
    {
    }

    public class ChildActor : ReceiveActor, IChildActor, IWithUnboundedStash
    {
        public IStash Stash { get; set; }
    }

    public static class ActorHelper
    {
        public static IActorRef CreateActor(IUntypedActorContext context, Type actorType, string name)
        {
            var actorName = actorType.Name + "." + name;

            var actor = context.Child(actorName);
            if (!actor.Equals(ActorRefs.Nobody))
            {
                return actor;
            }

            var actorProps = context.DI().Props(actorType);
            actor = context.ActorOf(actorProps, actorName);

            return actor;
        }
    }
}
