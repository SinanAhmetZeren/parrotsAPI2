#!/bin/bash

open -a Terminal

osascript <<'EOF'
tell application "Finder"
    set screenBounds to bounds of window of desktop
    set sw to item 3 of screenBounds
    set sh to item 4 of screenBounds
end tell

set hw to sw / 2
set h1 to sh / 8
set h3 to (sh * 3) / 8
set h2 to sh / 4
set h35 to (sh * 7) / 16
set menuBar to 25

tell application "Terminal"
    -- SSH tunnel - left, 1/8
    do script "ssh -N -L 5432:127.0.0.1:5432 deploy@168.119.119.25"
    delay 0.5
    set bounds of front window to {0, menuBar, hw, menuBar + h1}
    delay 0.5

    -- API - left, 3/8
    do script "cd /Users/ahmetzeren/Repos/parrotsAPI2 && dotnet run"
    delay 0.5
    set bounds of front window to {0, menuBar + h1, hw, menuBar + h1 + h3}
    delay 0.3

    -- Web - left, 1/8
    do script "cd /Users/ahmetzeren/Repos/parrotsWeb && BROWSER=none npm start"
    delay 0.5
    set bounds of front window to {0, menuBar + h1 + h3, hw, menuBar + h1 + h3 + h1}
    delay 0.3

    -- ngrok - left, 2/8
    do script "pkill ngrok; sleep 1; ngrok http --domain=adapting-sheepdog-annually.ngrok-free.app https://localhost:7151"
    delay 0.5
    set bounds of front window to {0, menuBar + h1 + h3 + h1, hw, menuBar + h1 + h3 + h1 + h2}
    delay 0.3

    -- Mobile 1 - right, 3.5/8
    do script "cd /Users/ahmetzeren/Repos/parrotsapp && npx expo start --port 8081"
    delay 0.5
    set bounds of front window to {hw, menuBar, sw, menuBar + h35}
    delay 0.3

    -- Mobile 2 - right, 3.5/8
    do script "cd /Users/ahmetzeren/Repos/parrotsapp && npx expo start --port 8082"
    delay 0.5
    set bounds of front window to {hw, menuBar + h35, sw, menuBar + h35 + h35}
end tell
EOF
