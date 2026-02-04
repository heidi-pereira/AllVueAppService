# Redux

Redux is our app state management tool.
React context is generally being phased out (though may still serve as a dependency injection mechanism).
More general tips are available on the [TechWiki page](https://github.com/Savanta-Tech/TechWiki/wiki/Redux)

## Where not to redux

Generic components (over-time chart, dropdown, etc.) should not reference redux since they should not be aware of their position/purpose in the app.
As always, such components should be passed appropriate props by their parents to render and trigger on user interaction.

## App state aware components

The idea is to separate transformations/actions out, so that these components *only*:
* Render based on the current props and slice state
* Dispatch the minimal update representing a user action

### Why

* Historically, these components have ended up doing all sorts of logic and effects: transformations, api calls, etc.
* Rendered components are the hardest/clunkiest thing to test, so having such behaviour in there made testing hard and hence rarely done.
* We've vastly increased our test coverage, and the ease of writing tests by separating this logic out.

### Where do transformations/actions go?

Transformations go into selectors. For more complex situations you may need/want a hook.
The most common case for needing a hook is depending on location. All URL syncing happens in [UrlSync.tsx](https://github.com/Savanta-Tech/Vue/blob/main/src/BrandVue.FrontEnd/client/components/helpers/UrlSync.tsx#L232) to avoid different hooks fighting over making updates:
Helper functions should be moved elsewhere to keep it tidy so that UrlSync just contains two UseEffect calls:
* The first UseEffect detects changes to location and updates dispatches actions to change the state
* The second UseEffect detects changes to state and updates the location.
