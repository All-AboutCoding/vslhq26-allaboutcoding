# Credits & Third-Party Notices

Fantasy AI Adventure is released under the [MIT License](./LICENSE).
This file lists the third-party components the project uses at runtime,
along with their licenses and any attribution requirements.

No third-party source code, datasets, fonts, images, or audio files are
bundled or redistributed with this repository. All third-party components
below are consumed at runtime over the network or via a locally-installed
runtime the end user provides.

---

## 1. .NET 10 (Base Class Library)

- **Component:** .NET runtime / BCL (`net10.0` target)
- **Publisher:** Microsoft / .NET Foundation
- **License:** MIT
- **Reference:** https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
- **Notes:** No redistribution — end user supplies the .NET 10 SDK/runtime.

## 2. NuGet packages

- **None.** `AdventureGame.csproj` contains no `<PackageReference>` entries.

---

## 3. Llama 3.2 (AI model — "Built with Llama")

- **Component:** `llama3.2` open-weight language model, invoked locally via Ollama.
- **Publisher:** Meta Platforms, Inc.
- **License:** Llama 3.2 Community License Agreement
- **License URL:** https://www.llama.com/llama3_2/license/
- **Acceptable Use Policy:** https://www.llama.com/llama3_2/use-policy/
- **Attribution (required by license §5):**

  > "Built with Llama"
  >
  > Llama 3.2 is licensed under the Llama 3.2 Community License,
  > Copyright © Meta Platforms, Inc. All Rights Reserved.

- **Notes:**
  - Model weights are downloaded and run **locally by the end user** via
	Ollama (`ollama pull llama3.2`). This project does not redistribute
	the weights.
  - Users of this project must comply with Meta's Acceptable Use Policy.
  - Commercial-use restriction: services with >700M monthly active users
	require a separate license from Meta. This does not affect hackathon
	or personal use.

## 4. Ollama (local model runtime)

- **Component:** Ollama, called via HTTP at `http://localhost:11434`.
- **License:** MIT
- **Reference:** https://github.com/ollama/ollama/blob/main/LICENSE
- **Notes:** Installed and run by the end user; not redistributed.

## 5. Pollinations.ai (image generation service)

- **Component:** `https://image.pollinations.ai/prompt/{prompt}` HTTP endpoint.
- **License / Terms:** Free public API, no key required. Underlying image
  models are operated by Pollinations.
- **Reference:** https://pollinations.ai/
- **Notes:**
  - No image assets from Pollinations are bundled in this repository.
	Images are generated at runtime and saved to the end user's local
	Pictures folder.
  - Ownership / usage rights of generated images are governed by
	Pollinations' terms at the time of generation.

---

## 6. AI-generated code

Portions of this project's source code were authored or assisted by
**GitHub Copilot**. Per GitHub's Terms of Service, Copilot suggestions
accepted by the user are the user's to license as they see fit; those
contributions are covered by this repository's MIT license.

## 7. Sample code from third-party sources

None. No code was copied from Stack Overflow, blog posts, or other
third-party sources that carry attribution or share-alike requirements
(e.g. CC BY-SA).

---

## Reporting a compliance issue

If you believe a third-party component is missing from this file or is
attributed incorrectly, please open an issue on the repository.
