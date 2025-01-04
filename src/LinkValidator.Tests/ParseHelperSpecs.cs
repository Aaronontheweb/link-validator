// -----------------------------------------------------------------------
// <copyright file="ParseHelperSpecs.cs">
//      Copyright (C) 2025 - 2025 Aaron Stannard <https://aaronstannard.com/>
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using LinkValidator.Actors;
using LinkValidator.Util;

namespace LinkValidator.Tests;

public class ParseHelperSpecs
{
    // create a string that contains HTML linking to a few different URLs using relative links
    private const string RelativeHtml = """
                                        
                                                <html>
                                                    <head>
                                                        <title>Test Page</title>
                                                    </head>
                                                    <body>
                                                        <a href="/about">About</a>
                                                        <a href="/contact">Contact</a>
                                                        <a href="/faq">FAQ</a>
                                                    </body>
                                                </html>
                                        """;

    [Fact]
    public void ParseHelper_should_return_absolute_uris()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("http://example.com"));

        // Act
        var uris = ParseHelpers.ParseLinks(RelativeHtml, uri);

        // Assert
        uris.Should().HaveCount(3);
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/contact")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/faq")));
    }

    // create a string that contains HTML linking to a few different URLs using absolute links
    private const string AbsoluteHtml = """
                                        
                                                <html>
                                                    <head>
                                                        <title>Test Page</title>
                                                    </head>
                                                    <body>
                                                        <a href="http://example.com/about">About</a>
                                                        <a href="http://example.com/contact">Contact</a>
                                                        <a href="http://example.com/faq">FAQ</a>
                                                    </body>
                                                </html>
                                        """;

    [Fact]
    public void ParseHelper_should_return_absolute_uris_when_given_absolute_links()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("https://example.com")); // using a different scheme (HTTPS)

        // Act
        var uris = ParseHelpers.ParseLinks(AbsoluteHtml, uri);

        // Assert
        uris.Should().HaveCount(3);

        // notice that we convert the scheme to https
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/contact")));
        uris.Should().Contain(new AbsoluteUri(new Uri("https://example.com/faq")));
    }

    private const string MixedHtml = """
                                     
                                             <html>
                                                 <head>
                                                     <title>Test Page</title>
                                                 </head>
                                                 <body>
                                                     <a href="/about">About</a>
                                                     <a href="http://example.com/contact">Contact</a>
                                                     <a href="http://fakeurl.com/faq">FAQ</a>
                                                 </body>
                                             </html>
                                     """;

    [Fact]
    public void ParseHelper_should_return_absolute_uris_when_given_mixed_links()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("http://example.com"));

        // Act
        var uris = ParseHelpers.ParseLinks(MixedHtml, uri);

        // Assert
        uris.Should().HaveCount(2); // don't count the FAKEURL one
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/contact")));
    }

    public const string TweetShareLink = """

                                         <!DOCTYPE html>
                                         <html class='no-js' lang='en'>
                                             <head>

                                         <meta charset="utf-8">
                                         <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1">
                                         <meta content='width=device-width, initial-scale=1.0' name='viewport'>
                                         <link href='//fonts.googleapis.com/css?family=PT+Sans:400,700,400italic,700italic|Droid+Serif:400,700,400italic,700italic|PT+Serif:400,700,400italic' rel='stylesheet' type='text/css'>
                                         <link rel="shortcut icon" href="/images/favicon.ico" type="image/x-icon">
                                         <link rel="icon" href="/images/favicon.ico" type="image/x-icon">
                                         <link rel="apple-touch-icon" href="/images/iphone-icon.png"/>








                                         <link rel="canonical" href="https://petabridge.com/blog/high-optionality-programming-pt1/">
                                         
                                          <!-- Open Graph tags -->
                                             <meta content="" property="og:site_name">
                                             
                                               <meta content="High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1" property="og:title">
                                             
                                             
                                               <meta content="article" property="og:type">
                                             
                                             
                                               <meta content="Creating Software that Preserves Choice." property="og:description">
                                             
                                             
                                               <meta content="https://petabridge.com/blog/high-optionality-programming-pt1/" property="og:url">
                                             
                                             
                                               <meta content="2021-09-15T23:30:00+00:00" property="article:published_time">
                                               <meta content="https://petabridge.com/about/" property="article:author">
                                             
                                             
                                               <meta content="" property="og:image">
                                             
                                             
                                               
                                               <meta content="Akka.NET" property="article:section">
                                               
                                             
                                             
                                               
                                             
                                         
                                             <!-- Twitter card -->
                                             <meta name="twitter:card" content="summary">
                                             <meta name="twitter:site" content="@Petabridge">
                                             <meta name="twitter:creator" content="@Petabridge">
                                             
                                               <meta name="twitter:title" content="High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1">
                                             
                                             
                                               <meta name="twitter:url" content="https://petabridge.com/blog/high-optionality-programming-pt1/">
                                             
                                             
                                               <meta name="twitter:description" content="High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1">
                                             
                                             
                                               <meta name="twitter:image:src" content="">
                                             
                                           </head>

                                         <!--[if lt IE 9]>
                                         <script src="//html5shiv.googlecode.com/svn/trunk/html5.js"></script>
                                         <![endif]-->



                                         <title>High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1 | Petabridge</title>

                                         <link rel="alternate" type="application/rss+xml" title="Petabridge" href="/blog/feed.xml" />

                                         <link href='/css/style.css' rel='stylesheet'>
                                         <script src='/js/modernizr.js'></script>

                                         <script type="application/ld+json">
                                         { "@context" : "http://schema.org",
                                           "@type" : "Organization",
                                           "name" : "Petabridge",
                                           "url" : "https://petabridge.com",
                                           "sameAs" : [
                                             "https://twitter.com/petabridge",
                                             "https://facebook.com/petabridge",
                                             "",
                                             "https://www.youtube.com/@petabridge?sub_confirmation=1",
                                             "https://github.com/petabridge",
                                             "https://www.linkedin.com/company/petabridge/"
                                           ]
                                         }
                                         </script></head>
                                             <body class="  blog smoothscroll">
                                               
                                                 <div id="flash-message-container"></div>
                                               
                                         
                                               <div id="loading" class="hidden text-center">
                                                 <i class="fa text-white fa-spinner fa-pulse fa-3x fa-fw"></i>
                                               </div>
                                         
                                               
                                         
                                               
                                                 
                                                   <div class='header contain-to-grid clearfix sticky '>
                                           <nav class='top-bar' data-options='sticky_on: large' data-topbar=''>
                                             <ul class='title-area'>
                                               <li class='name'>
                                                 <h1>
                                                   <a class="nav-site-name" href='/'>
                                                     <img src='/images/logo.png' class="logo nav-logo">
                                                     Petabridge
                                                   </a>
                                                 </h1>
                                               </li>
                                               <li class='toggle-topbar menu-icon'>
                                                 <a href='#'></a>
                                               </li>
                                             </ul>
                                             <section class='top-bar-section'>
                                               <ul class="right">
                                         
                                         
                                           
                                           
                                         
                                           
                                             <li class="nav-item has-dropdown ">
                                               <a href="/services">Akka.NET Services</a>
                                               <ul class='dropdown'>
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/services/support">Akka.NET Support Plans</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/services/consulting">Akka.NET Architecture and Design Review</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/services/consulting#code-review">Akka.NET Code Review</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/services/consulting#implementations">Akka.NET Implementation and Development</a>
                                                     </li>
                                                   
                                                 
                                               </ul>
                                             </li>
                                           
                                         
                                         
                                           
                                           
                                         
                                           
                                             <li class="nav-item has-dropdown ">
                                               <a href="/training">Akka.NET Training</a>
                                               <ul class='dropdown'>
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/training">Akka.NET Training</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/bootcamp">Akka.NET Bootcamp</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/training/onsite-training">Custom Akka.NET Training</a>
                                                     </li>
                                                   
                                                 
                                               </ul>
                                             </li>
                                           
                                         
                                         
                                           
                                           
                                         
                                           
                                             <li class="nav-item has-dropdown ">
                                               <a href="/blog">Blog</a>
                                               <ul class='dropdown'>
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/blog/">Latest posts</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/blog/start">New? Start here</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="/blog/archive">Archives</a>
                                                     </li>
                                                   
                                                 
                                               </ul>
                                             </li>
                                           
                                         
                                         
                                           
                                           
                                         
                                           
                                             <li class="nav-item has-dropdown ">
                                               <a href="">Products</a>
                                               <ul class='dropdown'>
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="https://phobos.petabridge.com/">Akka.NET Performance Monitoring - Phobos</a>
                                                     </li>
                                                   
                                                 
                                                   
                                                     <li>
                                                       <a class="dropdown-item" href="https://cmd.petabridge.com/">Akka.NET Commandline Management - Petabridge.Cmd</a>
                                                     </li>
                                                   
                                                 
                                               </ul>
                                             </li>
                                           
                                         
                                         
                                           
                                           
                                         
                                           
                                             <li class="nav-item active">
                                               <a href="https://help.petabridge.com/">Login</a>
                                             </li>
                                           
                                         
                                         </ul>
                                             </section>
                                           </nav>
                                         </div>
                                                 
                                               
                                         
                                               <div class='full'>
                                           <div class='row'>
                                             <div class='special-title blog-header centered-text'>
                                               <i class='fa fa-code'></i>
                                               <a href="/blog">
                                                 <h2>Beyond HTTP</h2>
                                                 <p>Open Source Distributed Systems in .NET</p>
                                               </a>
                                               <p class='shortline'></p>
                                             </div>
                                             <div class='spacing'></div>
                                           </div>
                                           <div class='row'>
                                             <div class='large-12 columns'>
                                               <div class='post'>
                                           <div class='info'>
                                             <h1><a href='/blog/high-optionality-programming-pt1/'>High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1</a></h1>
                                             <h3 class="subheader">Creating Software that Preserves Choice.</h3>
                                           </div>
                                           <div class='content clearfix'>    
                                             <p>A concept I’ve been trying to put a name to is software application architectures that inherently lend themselves to a lower rate of technical debt accumulation than others. This post is my first attempt to do that.</p>

                                         <h2 id="technical-debt">Technical Debt</h2>
                                         <p>Technical debt is a term that is used frequently in our industry and while its meaning is commonly understood among experienced technologists, it’s not clearly defined.</p>

                                         <p>In the abstract:</p>

                                         <ul>
                                           <li>Technical debt is cost incurred from software design and implementation choices made in the past;</li>
                                           <li>Interest on that technical debt accrues and compounds as a result of subsequent decisions that are layered onto the original set of choices; and</li>
                                           <li>The full cost of technical debt is not known until, at some point in the future, there is a need to modify the software system which forces the development team to <em>modify the original choices built into the existing system</em> and calculate the level of effort needed to change them safely.</li>
                                         </ul>

                                         <!-- more -->

                                         <p>Here’s what makes technical debt so complicated to pin down - its real cost depends upon <em>what might happen later</em> in the future, due to changing circumstances, environments, business requirements, and so on. These can be really difficult to anticipate at the onset of a greenfield software project even for well-intentioned, experienced, and disciplined programming teams.</p>

                                         <h3 id="example-database-driven-development">Example: Database-Driven Development</h3>
                                         <p>One the most painful experiences of my software development career was at my last startup, <a href="https://aaronstannard.com/makedup-analytics-stack/">MarkedUp, where we had to migrate off of RavenDb and onto Apache Cassandra under <em>dire</em> circumstances</a> in early 2013.</p>

                                         <p>Our service had only been live for maybe 45 days, but in that time we’d successfully acquired <em>a ton</em> of new users in a very short period of time. Way beyond our most optimistic expectations - consecutive, compound 200-400% activity growth over several 3-4 days. Going from about 10k events per day to about 5-8 million. And even though we’d thoroughly tested our software, there wasn’t much data available on how RavenDb’s MapReduce indicies would perform over time. We were early adopters.</p>

                                         <p>As we discovered, RavenDb collapsed under a modest amount of traffic, 30 writes per second or so, and the MapReduce indicies we’d used to power many of our “real-time” analytics would simply fail to update for days at a time. The only solution we found to fix the indicies was to continuously migrate from one very large EC2 instance to another using a homegrown migration tool since Raven’s would collapse under load. That would give the indicies a chance to catch up on the new system as newer data got migrated first - and is more valuable in a real-time analytics system.</p>

                                         <blockquote>
                                           <p>NOTE: RavenDb is assuredly a better technology now, but it was total disaster back then. Don’t judge RavenDb today by how it performed back in early 2013.</p>
                                         </blockquote>

                                         <p>But where the technical debt came into the picture: I listened to some of RavenDb creator Ayende’s advice and took it to heart. From his post, “<a href="https://ayende.com/blog/3955/repository-is-the-new-singleton">Repository is the new Singleton</a>”</p>

                                         <blockquote>
                                           <p>So, what do I gain by using the repository pattern when I already have NHibernate (or similar, most OR/M have matching capabilities by now)?</p>
                                         
                                           <p>Not much, really, expect as additional abstraction. More than that, the details of persistence storage are:</p>
                                         
                                           <ul>
                                             <li>Complex</li>
                                             <li>Context sensitive</li>
                                             <li>Important</li>
                                             <li>
                                               <p>Trying to hide that behind a repository interface usually lead us to a repository that has method like:</p>
                                             </li>
                                             <li>FindCustomer(id)</li>
                                             <li>FindCustomerWithAddresses(id)</li>
                                             <li>FindCustomerWith..
                                         It get worse when you have complex search criteria and complex fetch plan. Then you are stuck either creating a method per each combination that you use or generalizing that. Generalizing that only means that you now have an additional abstraction that usually map pretty closely to the persistent storage that you use.</li>
                                           </ul>
                                         
                                           <p>From my perspective, that is additional code that doesn’t have to be written.</p>
                                         </blockquote>

                                         <p>So that’s exactly what we did - embedded RavenDb-specific driver code in all of our HTTP methods, ingestion API, and so forth. No more of the classic <code class="language-plaintext highlighter-rouge">IRepository</code> pattern for us. The moment we decided to do that was the moment where I ended up, unwittingly, choosing to spend hundreds of thousands of investor dollars migrating off of RavenDb versus a much smaller number had I made some different choices.</p>

                                         <p>Let’s separate two components of the cost:</p>

                                         <ol>
                                           <li>The cost of migrating current data from one database to another - technical debt but in a class of its own. Even between two identical T-SQL systems there is an inescapable cost to migrating data from one instance to another. You have to create a process to do the transform, account for errors, re-transmit the failed parts, eliminate duplicates, and so on.</li>
                                           <li>The cost of implementing application’s current read / write models - <strong>this</strong> is where the technical debt primarily accumulates in this scenario.</li>
                                         </ol>

                                         <p>The repository pattern is basically an abstraction designed to create <a href="https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/june/the-unit-of-work-pattern-and-persistence-ignorance">persistence ignorance</a> - a principle long-recommended in the early to late 2000s for allowing application developers to create and test their business logic and their persistence models independently. It has, at least in .NET Twitterland, fallen out of favor and been replaced with exactly what we did at MarkedUp: programming directly against the first party features of the database.</p>

                                         <p>The benefits of doing this are, as explained in the blog above, that it’s easier to implement complex read/write models and it’s one less layer of abstraction that needs to be written and maintained.</p>

                                         <p>But as we discovered, the downsides to developing directly against the database are <em>massive</em> from a technical debt perspective:</p>

                                         <ol>
                                           <li>Because your read and write models closely follow the particulars of the database you chose, your code is effectively married to it - in the event that your needs change or your database fails to grow with you - then <em>you are screwed</em>.</li>
                                           <li>Integration testing is the only realistic option for testing your code, as it’s married to your database, and your business logic isn’t truly independent of your persistence model.</li>
                                           <li>Without a set of shared abstractions that distill down your persistence patterns into some lowest common denominators, you instead have bespoke use-case specific persistence code everywhere - and rewriting all of it everywhere at once is extremely high risk and expensive.</li>
                                         </ol>

                                         <p>Programming directly against a database is a high-risk, low-reward bet: if everything goes right, you have one less layer of code to understand and one less common abstraction for developers to bicker about. All you have to do is sacrifice testability, a type-safe way of enforcing standardized approaches to reads / writes, and pray that nothing goes wrong - because if it does, you have 10s to 100s of bespoke driver invocations embedded directly into your application code that have to be replaced together in large groups, if not all at once.</p>

                                         <p>This is why the wisdom of persistence ignorance is trumpeted by the experienced web programmers of the 1990s: this has been tried before and failed spectacularly. NoSQL and distributed K/V databases doesn’t change that.</p>

                                         <p>Marrying your application code, your read / write models, and really your business domain to a specific OR/M or database implementation is a classic technical debt creator: it’s an assumption that the database will continue to grow with the software in perpetuity. It doesn’t price in the possibility, or in many cases - the inevitability, that this will not hold true.</p>

                                         <p>This is a decision that destroys optionality - the ability to preserve future choices for the software without rewriting it.</p>

                                         <h2 id="optionality">Optionality</h2>
                                         <p>I used the example of database-driven programming as an example of optionality destruction or “low optionality programming.” It’s an inflexibility built into the system from its inception - and if you never run into an instance where that inflexibility becomes an impediment to implementing a future change in your software, then you don’t have any technical debt. But as was the case with MarkedUp, that inflexibility can become a highly compounded source of technical debt that demanded a high price in time, stress, and dollars to be repaid.</p>

                                         <p><strong>This is the context of optionality and its role in reducing technical debt</strong>: it is the upfront decision to “price in” future changes to the software and design the system to be able to support them. The nature of those future changes is not definitively known or agreed upon at the conception of the system but it is accepted that their arrival is highly probable.</p>

                                         <h3 id="definition">Definition</h3>
                                         <p>“Optionality” is a term I most often come across in finance, i.e. stock options, but I’m going to attempt to explain it on my own terms. Optionality simply means “to have options,” but in order to have an option you must:</p>

                                         <ol>
                                           <li><strong>Pay a premium</strong> - i.e. a reservation fee, a deposit, or any other kind of upfront cost;</li>
                                           <li><strong>An exercise cost</strong> - a <em>fixed</em> cost to exercise the option, agreed upon at the time it’s created;</li>
                                           <li><strong>A right to exercise</strong> - a set of agreements on when and how you can exercise your option, established at time you paid the premium; and</li>
                                           <li><strong>An expiration date</strong> - options don’t last forever; an invitation to speak at a conference only lasts so long as the request for papers is still open.</li>
                                         </ol>

                                         <p>If you receive stock options in the company you work for as part of your annual compensation: your “premium” is your on-the-job performance; your exercise cost is literally the exercise or strike price of the stock; your right to exercise is subject to the vesting schedule; and your expiration date is the length of your exercise window, usually 10 years or so.</p>

                                         <p>The expiration date is the key - the further in the future the expiration date, the more valuable the option is, and usually - the higher the premium.</p>

                                         <h3 id="optionality-and-technical-debt">Optionality and Technical Debt</h3>
                                         <p>In programming, the premium we pay is the set of upfront development costs at the onset of a new project. If you want to get a rapid prototype into production quickly, as we did at MarkedUp, you’re going to pay a lower premium at the cost of having more expensive future choices - technical debt.</p>

                                         <p>Technical debt is the price you <em>might</em> pay later; optionality is the price you <em>will</em> pay now in order to reduce possible future cost.</p>

                                         <p>That’s the bet - and you are always making it every day on the job, knowingly or not.</p>

                                         <p>If you’ve been in the industry a number of years you have likely learned how to make rapid prototypes - often a necessity on the job. That’s a quick skill to learn because of its immediacy. Learning how to do the opposite, to plan for anticipated but not fully qualified future changes, is a less easy-to-practice skill because it requires planning out the evolution of a software system over many years and sticking around long enough to see which bets paid off and which ones did not.</p>

                                         <p>Let’s revisit optionality in terms of software:</p>

                                         <ol>
                                           <li><strong>Pay a premium</strong> - this is your upfront development cost to complete a feature, build a V1, etc…</li>
                                           <li><strong>An exercise cost</strong> - a planned route you can take to implement one of several possible types of changes in your system; you’ve thought through, ahead of time, how these types of changes can be introduced to the system gradually and have bounded their costs;</li>
                                           <li><strong>A right to exercise</strong> - you can exercise at any time prior to expiration;</li>
                                           <li><strong>An expiration date</strong> - here’s the great part: technology options are good for as long as the original component is relevant to the software and the business supporting it.</li>
                                         </ol>

                                         <p>Software options have a long, long expiration date - this is the power center of high optionality software.</p>

                                         <h2 id="patterns-for-creating-high-optionality-software">Patterns for Creating High Optionality Software</h2>
                                         <p>I am going to have to expand on this in a part 2, where I will detail the following recipes for building more optionality into your software:</p>

                                         <ol>
                                           <li>Prefer event-driven programming over remote procedure calls;</li>
                                           <li>Persistence ignorance is bliss, but event-sourcing is better;</li>
                                           <li>Command Query Responsibility Segregation;</li>
                                           <li>Apply functions over data - decouple stateful domain objects from business rules;</li>
                                           <li>Use actors to make systems dynamic, queryable, and recoverable; and</li>
                                           <li>Embrace extend-only design on schemas of any kind.</li>
                                         </ol>

                                         <p>Many of you reading this may be predisposed, one way or another, to some of these ideas already. My goal in writing this is not to convince you that you that your current way of writing software is wrong or even that it can be improved. My goal and Petabridge’s is to expand the power of software developers.</p>

                                         <p>My goal is to share a lexicon and tools to prepare for the long-term evolution of our software systems - to make our tradeoffs planned and intentional rather than done out of habit. And most importantly - to explore why paying a small premium today might create tremendous dividends for you and your team members tomorrow.</p>

                                         <p>I plan to have our second installment of this post done soon. <a href="https://petabridge.us12.list-manage.com/subscribe/post?u=2a1aa40fed72ca1a3b371515e&amp;id=cb6592d396&amp;SIGNUP=blog">Subscribe for the next post</a>!</p>
                                         
                                         
                                             
                                             If you liked this post, you can
                                         <a href="https://twitter.com/intent/tweet?url=https://petabridge.com/blog/high-optionality-programming-pt1/&text=High Optionality Programming: Software Architectures that Reduce Technical Debt - Part 1&via=petabridge"
                                            target="_blank">
                                           share it with your followers</a>
                                         or
                                         <a href="https://twitter.com/petabridge">
                                           follow us on Twitter</a>!
                                         
                                             <div class="date">
                                               Written  by <a href="http://twitter.com/Aaronontheweb">Aaron Stannard</a> on September 15, 2021
                                             </div>
                                             
                                             <div><p>&nbsp;<p></div>
                                               <div id="more-posts" class="center">
                                         <ul class="inline-list">
                                         	<li>Read more about:</li>
                                         
                                          	<li><a href="/blog/category/akkadotnet/" rel="category">Akka.NET</a></span></li>
                                         
                                          	<li><a href="/blog/category/case-studies/" rel="category">Case Studies</a></span></li>
                                         
                                          	<li><a href="/blog/category/videos/" rel="category">Videos</a></span></li>

                                         </ul>
                                         </div>
                                             <div><p>&nbsp;<p></div>
                                             <div class="panel callout">
                                           <h3 class="text-center">Observe and Monitor Your Akka.NET Applications with Phobos</h3>
                                           <div class="row">
                                             <div class="small-2 columns">
                                               <a href="https://phobos.petabridge.com/" title="Phobos - instant observability for Akka.NET"/>
                                                 <img src="/images/phobos/phobos_profile_icon.png" alt="Phobos - instant OpenTelemetry observability for Akka.NET" width="90"/>
                                               </a>
                                             </div>
                                             <div class="small-10 columns">
                                               <p>Did you know that <a href="https://phobos.petabridge.com/" title="Phobos - instant observability for Akka.NET"/>Phobos</a> can automatically instrument your Akka.NET applications with OpenTelemetry?</p>
                                               <a class="button" href="https://phobos.petabridge.com/">Click here to learn more.</a>
                                             </div>
                                           </div>
                                         </div>
                                         
                                             
                                               
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         
                                         <div class="small-11 columns center">
                                         
                                             <div class="_form_1"></div><script src="https://petabridge.activehosted.com/f/embed.php?id=1" charset="utf-8"></script>

                                         </div>
                                         
                                             
                                           </div>
                                         </div>

                                         <div class='post small-10 columns center'>
                                           <a id='comments'></a>
                                           
                                         
                                         <div class="comments">
                                           <div id="disqus_thread"></div>
                                           <script type="text/javascript">
                                         
                                               var disqus_shortname = 'petabridge';
                                               var disqus_url = 'https://petabridge.com/blog/high-optionality-programming-pt1/';
                                         
                                               (function() {
                                                   var dsq = document.createElement('script'); dsq.type = 'text/javascript'; dsq.async = true;
                                                   dsq.src = '//' + disqus_shortname + '.disqus.com/embed.js';
                                                   (document.getElementsByTagName('head')[0] || document.getElementsByTagName('body')[0]).appendChild(dsq);
                                               })();
                                         
                                           </script>
                                           <noscript>Please enable JavaScript to view the <a href="http://disqus.com/?ref_noscript">comments powered by Disqus.</a></noscript>
                                         </div>

                                         </div>

                                         <script src="https://unpkg.com/mermaid@11.4.1/dist/mermaid.min.js"></script>
                                         <script>
                                           document.addEventListener("DOMContentLoaded", function () {
                                             // Initialize Mermaid
                                             mermaid.initialize({
                                               startOnLoad: true,
                                               theme: "dark",
                                             });
                                             // Find and render all elements with the class `language-mermaid`
                                             const mermaidBlocks = document.querySelectorAll('.language-mermaid');
                                             mermaidBlocks.forEach((block) => {
                                               mermaid.init(undefined, block);
                                             });
                                           });
                                         </script>
                                               
                                             </div>
                                           </div>
                                         </div>
                                         
                                               
                                                 
                                                   <div class='footer centered-text'>
                                           <div class='row'>
                                             <div class='large-12 columns'></div>
                                             <h1 class='light'>
                                               <img src='/images/logo.png' class="logo">
                                               <br>
                                               Petabridge
                                             </h1>
                                             <div class='socials centered-text'>
                                               <ul class="inline-list inline-block light">
                                                 <li><a href="/">Home</a></li>
                                                 <li><a href="/services">Services</a></li>
                                                 <li><a href="/training">Training</a></li>
                                                 <li><a href="/about">About</a></li>
                                                 <li><a href="/contact">Contact</a></li>
                                                 <li><a href="/blog">Blog</a></li>
                                                 <li><a href="/careers">Careers</a></li>
                                                 <li><a href="/press">Press</a></li>
                                                 <li><a href="/partners">Partners</a></li>
                                                 <li><a href="/legal/privacy.html">Privacy</a></li>
                                               </ul>
                                               <p class="light">Call us: 1-866-418-3140</p>
                                               <p class="light">Follow us on&hellip;</p>
                                               <a href='https://twitter.com/petabridge'>
                                                 <i class='fa fa-lg fa-twitter grey'></i>
                                               </a>
                                               <a href='https://www.youtube.com/@petabridge?sub_confirmation=1'>
                                                 <i class='fa fa-lg fa-youtube grey'></i>
                                               </a>
                                               <a href='https://facebook.com/petabridge'>
                                                 <i class='fa fa-lg fa-facebook grey'></i>
                                               </a>
                                               <a href='https://www.linkedin.com/company/petabridge/'>
                                                 <i class='fa fa-lg fa-linkedin grey'></i>
                                               </a>
                                               <a href='/blog/feed.xml'>
                                                 <i class='fa fa-lg fa-rss grey'></i>
                                               </a>
                                             </div>
                                             <div class="text-center" style="margin: 20px auto;">
                                           <a href='https://feedly.com/i/discover/sources/search/feed/Petabridge%3A%20Beyond%20HTTP'  target='blank'><img id='feedlyFollow' src='https://s3.feedly.com/img/follows/feedly-follow-rectangle-flat-big_2x.png' alt='follow us in feedly' width='131' height='56'></a>
                                         </div>
                                             <p class='copyright'>&copy; 2015-2025 Petabridge, LLC</p>
                                           </div>
                                         </div>
                                                 
                                               
                                         
                                               <script data-cfasync="false" src='/js/jquery.min.js'></script>
                                         
                                           <script src="/js/scroll.js" async></script>


                                         <script data-cfasync="false" src='/js/foundation.min.js'></script>
                                         <script data-cfasync="false" src='/js/moment.min.js'></script>
                                         <script data-cfasync="false" src='/js/moment-tz.min.js'></script>
                                         <script src="/js/main.js" async></script>





                                         <script>
                                         (function(i, s, o, g, r, a, m) {
                                             i['GoogleAnalyticsObject'] = r;
                                             i[r] = i[r] || function() {
                                                 (i[r].q = i[r].q || []).push(arguments)
                                             }, i[r].l = 1 * new Date();
                                             a = s.createElement(o),
                                                 m = s.getElementsByTagName(o)[0];
                                             a.async = 1;
                                             a.src = g;
                                             m.parentNode.insertBefore(a, m)
                                         })(window, document, 'script', '//www.google-analytics.com/analytics.js', 'ga');

                                         ga('create', "UA-58505899-1", 'auto');
                                         ga('send', 'pageview');
                                         ga('require', 'ecommerce');
                                         </script>



                                         <!-- <script src="//platform.twitter.com/oct.js" type="text/javascript"></script>
                                         <script type="text/javascript">
                                         twttr.conversion.trackPid('l5m3t', { tw_sale_amount: 0, tw_order_quantity: 0 });</script>
                                         <noscript>
                                         <img height="1" width="1" style="display:none;" alt="" src="https://analytics.twitter.com/i/adsct?txn_id=l5m3t&p_id=Twitter&tw_sale_amount=0&tw_order_quantity=0" />
                                         <img height="1" width="1" style="display:none;" alt="" src="//t.co/i/adsct?txn_id=l5m3t&p_id=Twitter&tw_sale_amount=0&tw_order_quantity=0" /></noscript> -->
                                         <script type="text/javascript">
                                             var $mcGoal = {'settings':{'uuid':'2a1aa40fed72ca1a3b371515e','dc':'us12'}};
                                             (function() {
                                                  var sp = document.createElement('script'); sp.type = 'text/javascript'; sp.async = true; sp.defer = true;
                                                 sp.src = ('https:' == document.location.protocol ? 'https://s3.amazonaws.com/downloads.mailchimp.com' : 'http://downloads.mailchimp.com') + '/js/goal.min.js';
                                                 var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(sp, s);
                                             })(); 
                                         </script>

                                         <script type="text/javascript">
                                             (function(e,t,o,n,p,r,i){e.visitorGlobalObjectAlias=n;e[e.visitorGlobalObjectAlias]=e[e.visitorGlobalObjectAlias]||function(){(e[e.visitorGlobalObjectAlias].q=e[e.visitorGlobalObjectAlias].q||[]).push(arguments)};e[e.visitorGlobalObjectAlias].l=(new Date).getTime();r=t.createElement("script");r.src=o;r.async=true;i=t.getElementsByTagName("script")[0];i.parentNode.insertBefore(r,i)})(window,document,"https://diffuser-cdn.app-us1.com/diffuser/diffuser.js","vgo");
                                             vgo('setAccount', '1003137058');
                                             vgo('setTrackByDefault', true);
                                         
                                             vgo('process');
                                         </script>
                                         <script type="text/javascript">
                                         _linkedin_partner_id = "481314";
                                         window._linkedin_data_partner_ids = window._linkedin_data_partner_ids || [];
                                         window._linkedin_data_partner_ids.push(_linkedin_partner_id);
                                         </script><script type="text/javascript">
                                         (function(){var s = document.getElementsByTagName("script")[0];
                                         var b = document.createElement("script");
                                         b.type = "text/javascript";b.async = true;
                                         b.src = "https://snap.licdn.com/li.lms-analytics/insight.min.js";
                                         s.parentNode.insertBefore(b, s);})();
                                         </script>
                                         <noscript>
                                         <img height="1" width="1" style="display:none;" alt="" src="https://dc.ads.linkedin.com/collect/?pid=481314&fmt=gif" />
                                         </noscript>
                                         
                                         
                                             </body>
                                         </html>
                                         """;

    /*
     * The bug here is caused by the fact that the QUERYSTRING in the Twitter share Uri is not well-encoded,
     * which causes us to accidentally:
     *
     * 1. Not detect that this is already an absolute Uri
     * 2. Create a malformed absolute Uri later
     *
     * This is the type of thing that we will probably see frequently when scanning websites. To make the tool less
     * noisy we should probably only look at the front part of the url when determining whether it's absolute or not.
     *
     * Ideally, websites won't emit mal-formed urls, but we can't really control that.
     */
    [Fact]
    public void ParseHelper_should_not_include_absoluteUris_that_appear_in_querystring()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("https://petabridge.com/"));

        // Act
        var uris = ParseHelpers.ParseLinks(TweetShareLink, uri);

        // Assert
        uris.Should().HaveCount(22);
    }

    private const string LinkFragmentsHtml = """
                                             
                                                     <html>
                                                         <head>
                                                             <title>Test Page</title>
                                                         </head>
                                                         <body>
                                                             <a href="/about">About</a>
                                                             <a href="http://example.com/contact">Contact</a>
                                                             <a href="http://example.com/contact#phone">Contact Phone Numbers</a>
                                                         </body>
                                                     </html>
                                             """;

    [Fact]
    public void ParseHelper_should_not_count_LinkFragments_separately()
    {
        // Arrange
        var uri = new AbsoluteUri(new Uri("http://example.com/"));

        // Act
        var uris = ParseHelpers.ParseLinks(LinkFragmentsHtml, uri);

        // Assert
        uris.Should().HaveCount(2);
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/about")));
        uris.Should().Contain(new AbsoluteUri(new Uri("http://example.com/contact")));
    }
}