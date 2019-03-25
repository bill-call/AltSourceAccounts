# AltSourceAccounts

Author: Bill Call wecall@prismnet.com

## What is this?

This Visual Studio 2017 solution is a partial implementation of an interview problem from AltSource. 
For reference the original "spec" follows:

>Please code a solution that can perform the following workflows through a console application (accessed via the command line) as well as through a web page. They don't have to run at the same time, but if you would like to do that, feel free!
>  
>-Create a new account  
>-Login  
>-Record a deposit  
>-Record a withdrawal  
>-Check balance  
>-See transaction history  
>-Log out  
>  
>Please use a temporary memory store (local cache) instead of creating an actual database.  
>  
>C# is preferred but not required. Use whatever frameworks/libraries you wish, and just make sure they are included or available via NuGet/npm/etc. Please don't spend much time on the UI (unless you love doing that).

I took more than a few liberties with the above.  There was a question in my mind, as to whether it was best to just do the absolute
minimum to satisfy the requirements, or should I take it more seriously. I opted for the latter. which has taken some time.  The idea
is to give the interviewer a better idea of my knowlege and capabilities.

## The Basic Architecture

The runtime environment consists of five major components:

1) An Identity Server, supporting OIDC via IdentityServer4, but running on top of the Microsoft Identity schema.  This Web Service
handles Authentication, Authorization, and hosts the UI to allow browser clients to register users and log in. It also supports machine-
friendly endpoints for adding new users.

2) AccountLib is the raw business logic and repository. It is not pretty, nor is it intended to be. All of the classes are
in a single file, for easy perusal. It has zero security features, with the exception that only admin users can do certain things.

3) AccountsApi is the Web Proxy for AccountsLib. It provides all the security, including authentication of users (human and machine).
If you are a client, you don't get end if you are not already known to the API. If you are human, you don't get in if you can't
authenticate yourself to the Identity Server. Even, then, you have the appropriate permissions (claims) in order to access any
given action.

4) AccountsCli is the console client. It follows standard OIDC flows to authenticate users and get the tokens it needs in order
to talk to AccountsApi. It uses the OIDC Resource Owner Pasword flow, so it is relatively insecure in that it must, at some point,
be in posessions of login credentials.

5) AcountsWeb is the browser client for AccountsAPI. It uses the Hybrid flow, so it is *never* in possession of any users's
credentials.  On the other hand, it involves browsers, so it has potential vulnerabilites to XSS and all the other gremlins
of browser-based applications.

Note that you can open as many CLI and/or Web consoles as you like, if you want to play with running concurrent sessions.

## How to Get Started

First, of course, clone this repo to your local environment.

This initial push contains all of the parts, but only the command line client is functional.  The easiest way to launch all the parts is
to just load the solution in VS 2017 and click the green "run" arrow but, due to the fact that the repository does not store the .suo file for this solution, you will need to configure the startup first:

1) Make sure the build configuration is "Debug".
2) Right-click the *Solution 'AltSourceAccounts'* entry at the top of the Solution Explorer.
3) Select "Properties" from very bottom the resulting menu.
4) Select Common Properties -> Startup Project.
5) Click the "Multiple startup projects" radio button.
6) Confgure IdentityServer, AccountsApi, AccountsWeb and Accounts Cli to "Start", in that order.  Leave everything else set to "None".
7) Click the "OK" button to save the changes.

At this point, the startup project (to the left of the green "Start" arrow in the toolbar) should say <Multiple Startup Projects>".

The debug solution is now configured to launch everything.  Clicking the "Start" button in the toolbar will automatically launch the Identity Service, Web and CLI clients, and the Accounts API.  Stick to debug here; trying to run in release mode will likely just get you tangled up in HTTPS issues. I haven't made any attempt to configure a release build.

At this point, you should have one browser tab open to the login screen of the Web Client, and two CLI windows.  One is the log
console for the Identity Server (that's the one that tells you to press CTRL-C to close it).  The other window is the CLI client
(that's the one with the "$" prompt, and nothing else).

## Using the Accounts CLI

The first thing to do is enter "-h" at the "$" prompt. This will allow you to see the top level of the command hierarchy. You can
enter partial commands, and add -h at the end to see what further options/arguments are available.  What I will do here, is walk
us through a sample session.

There are two admin users already configured: "bob" and "alice".  Both have "Pass123$" as their password. There's no differnce 
between them, but we'll be using alice. Just keep in mind that everything that follows would work just the same with bob.

    login alice Pass123$  
    create user foo Pass123$ Foo Bar  
    create account C00001  
    login foo Pass123$  
    account balance A00002  
    account history A00002  
    account credit A00002 200 -m Payday!  
    account debit A00002 25.50 -m "Bought Gas"  
    account debit A00002 10.25
    account balance A00002
    account history A00002
    logout

I've left out the responses that these commands will generate; they should be self-explanatory.

A couple of points of interest:

1) You can enter a command without logging in first. The console will prompt you for your credentials.

2) If you are logged in as one user, you can log in as another user without logging out first.  The console will automatically log you out.

3) Only admin users can use the "create" commmands.
4) Only non-admin users can use the "account" commands.

## The Web Client

Not ready yet.  You can create new users with it, but that's about it.

