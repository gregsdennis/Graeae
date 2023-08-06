# Community Engagement

Questions, suggestions, corrections.  All are welcome.

Channels for community engagement (where I'll be looking) include:

- Issues
- Slack (link in the README)

# Development

## Requirements

These libraries run tests in .Net 6, so you'll need that.

All of the projects are configured to use the latest C# version.

## IDE

I use Visual Studio Community with Resharper, and I try to keep everything updated.

Jetbrains Rider (comes with the Resharper stuff built-in), VS Code with your favorite extensions, or any basic text editor with a command line would work just fine.  You do you.

## Code Style & Releases

Please feel free to add any code contributions using your own coding style.  Trying to conform to someone else's style can be a headache and confusing, and I prefer working code over pretty code.  I find it's easier for contributors if I make my own style adjustments after a contribution rather than forcing conformance to my preferences.

Deployments to Nuget occur automatically upon merging with `main`, so I usually create a secondary branch where I can first update any package versions and release notes.

For these reasons, you can expect your PRs to be retargeted to a branch other than `main`.

## What Needs Doing?

Everything.

Outside of this, PRs are welcome.  For larger changes or changes to the API surface, it's preferred that there be some discussion in an issue before a PR is submitted, just to discuss the specifics of the change.  Mainly, I don't want you to feel like you've wasted your time if changes are requested or the PR is ultimately closed unmerged.
