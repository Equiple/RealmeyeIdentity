## RealmeyeIdentity
Service for authentication of RotMG players in third party services via OAuth.

Uses RealmEye page confirmation code during registration to verify player's account ownership.

## Implements registration flow
1. Third party service redirects to the RealmeyeIdentity page with a callback `redirectUri` query parameter;
2. User navigates to the registration page, RealmeyeIdentity generates a RealmEye confirmation code;
3. User pastes the confirmation code to their RealmEye page, enters RotMG name, new RealmeyeIdentity password, password confirmation;
4. RealmeyeIdentity validates the code on the player's RealmEye page, stores new user, redirects to the callback URI with an `authCode` query parameter;
5. Third party service exchanges provided `authCode` for an `idToken` using `POST GetToken` RealmeyeIdentity endpoint.

## Implements login flow
1. Third party service redirects to the RealmeyeIdentity page with a callback `redirectUri` query parameter;
2. User enters RotMG name, RealmeyeIdentity password;
4. RealmeyeIdentity validates the credentials, redirects to the callback URI with an `authCode` query parameter;
5. Third party service exchanges provided `authCode` for an `idToken` using `POST GetToken` RealmeyeIdentity endpoint.

## Other Equiple repos
* [Reverse proxy & docker compose](https://github.com/Equiple/Equiple-Proxy)
* [Web API](https://github.com/Equiple/Rotmg-Equiple-API)
* [Angular client](https://github.com/Equiple/Rotmg-Equiple-Client)
