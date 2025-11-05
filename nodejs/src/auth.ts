import { AccountInfo, AuthenticationResult, PublicClientApplication } from "@azure/msal-node";
import open from "open";
import { AuthOptions } from './models.js';

const scopes = ["499b84ac-1321-427f-aa17-267ca6975798/.default"];
const authority = "https://login.microsoftonline.com/common";
const clientId = "0d50963b-7bb9-4fe7-94c7-a99af00b5136";

class OAuthAuthenticator {

    private accountId: AccountInfo | null;
    private publicClientApp: PublicClientApplication;

    constructor() {
        this.accountId = null;
        this.publicClientApp = new PublicClientApplication({
            auth: { clientId, authority },
        });
    }

    public async getToken(): Promise<string> {
        let authResult: AuthenticationResult | null = null;

        if (this.accountId) {
            try {
                authResult = await this.publicClientApp.acquireTokenSilent(
                    { scopes, account: this.accountId, }
                );
            } catch (error) {
                authResult = null;
            }
        }

        if (!authResult) {
            authResult = await this.publicClientApp.acquireTokenInteractive({
                scopes,
                openBrowser: async (url) => { open(url); },
            });

            this.accountId = authResult.account;
        }

        if (!authResult.accessToken) {
            throw new Error("Failed to obtain Azure DevOps OAuth token.");
        }

        return authResult.accessToken;
    }
}

export function createAuthenticator(type?: string): () => Promise<AuthOptions> {
    switch (type) {
        case 'pat':
            const pat = process.env.AZURE_DEVOPS_PAT || '';

            if (!pat) {
                throw new Error("Personal Access Token (PAT) not found in AZURE_DEVOPS_PAT environment variable.");
            }

            return () => Promise.resolve({ type: 'pat', token: pat });
        case 'interactive':
        default:
            const authenticator = new OAuthAuthenticator();

            return async () => {
                const token = await authenticator.getToken();
                return Promise.resolve({ type: 'interactive', token });
            };
    }
}