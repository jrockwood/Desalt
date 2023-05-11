# Desalt
Converts Saltarelle C# files and projects to TypeScript.

**Hey Tableau/VCI** - Before you start using this codebase, I encourage you
to reach out to me. I might be able to help you out.

## History
The history of this project starts in the early 2000s while I was working at
Microsoft on the Excel Web Access project. I and my colleagues were tired of
writing and maintaining a large code base in raw JavaScript. I started looking
around for something to convert C# (which was our language of choice) into
JavaScript and found the work of a fellow Microsoftie, Nikhil Kothari. He had
written a tool called [Script#](https://github.com/nikhilk/scriptsharp). Over
the course of a few months, our team ported our existing raw JavaScript into
Script# code. We saw amazing productivity boosts since we could use Visual
Studio and C#, which were quite a bit more evolved and robust than the JavaScript
tools that were currently available. I also joined Nikhil Kothari's virtual
Script# team to implement new features.

I left Microsoft at the end of 2009 and joined [Tableau Software](http://tableau.com)
and repeated the same thing I had done at Microsoft - I helped port our existing
raw JavaScript to Script#, and saw the same benefits that we had seen on Excel.

Eventually the Script# project was abandoned (when Nikhil left Microsoft for
Google) and our team at Tableau moved to [Saltarelle](https://github.com/Saltarelle/SaltarelleCompiler),
which was designed to be a drop-in replacement for Script#, but added C# 5.0
language features.

In June 2015, Saltarelle was aquired by [Bridge.NET](http://bridge.net) but
compatibility with Saltarelle was not a design goal for the new project. We at
Tableau Software now faced a situation where we had a large code base using a
compiler that was no longer supported. Additionally, new code within Tableau was
being written predominately in TypeScript and we wanted a way to easily port our
existing Saltarelle-based code to TypeScript. Hence, Desalt, written in
my spare time over the course of several months.

## Status
The project is mostly working, but I don't yet have an entry point (either a CLI
or an MSBuild task). I have an end-to-end test that runs over real code taken
from our "Core" library at Tableau and am using that to implement features
little by little.

In short, this is not ready for prime time yet, although it does quite a bit
already. Here's a snapshot of a recent build with the C# on the left and the
TypeScript on the right.

![Desalt screen shot](DesaltScreenShot.png)

The remaining features are tracked [here](https://github.com/jrockwood/Desalt/projects/1).
