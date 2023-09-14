using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.Device;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	public class PrivacyDialogPresenter : UiToolkitPresenterData<PrivacyDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnAccept;
		}

		private Button _privacy;
		private Button _terms;
		private Button _confirm;
		private IGameServices _services;

		private const string DEFAULT_TERMS = "https://static.blastroyale.com/tos/TermsOfService.rtf";
		private const string DEFAULT_POLICY = "https://static.blastroyale.com/tos/PrivacyPolicy.rtf";
		
		protected override void QueryElements(VisualElement root)
		{
			_services = MainInstaller.ResolveServices();
			_terms = root.Q<Button>("TermsOfServiceButton");
			_privacy = root.Q<Button>("PrivacyButton");
			
			_services.GameUiService.LoadUiAsync<GenericScrollingTextDialogPresenter>();
			
			_confirm = root.Q<Button>("ConfirmButton").Required();
			_confirm.clicked += Data.OnAccept;
			var data = _services.DataService.GetData<AppData>().TitleData;

			_terms.clicked += () =>
			{
				data.TryGetValue("TERMS_OF_SERVICE_URL", out var termsUrl);
				_ = DownloadAndShow("Terms of Service", termsUrl, HardCodedTexts.TERMS_OF_SERVICE);
			};
			
			_privacy.clicked += () =>
			{
				data.TryGetValue("PRIVACY_POLICY_URL", out var termsUrl);
				_ = DownloadAndShow("Privacy Policy", termsUrl, HardCodedTexts.PRIVACY_POLICY);
			};
		}

		protected override async Task OnClosed()
		{
			await base.OnClosed();
			await _services.GameUiService.CloseUi<GenericScrollingTextDialogPresenter>(true);
			_services.GameUiService.UnloadUi<GenericScrollingTextDialogPresenter>();
		}
		
		private async Task DownloadAndShow(string title, string url, string defaultValue)
		{
			var data = new GenericScrollingTextDialogPresenter.StateData()
			{
				Title = title,
				OnConfirm = () => _services.GameUiService.CloseUi<GenericScrollingTextDialogPresenter>()
			};
			if (url == null)
			{
				data.Text = defaultValue;
			}
			else
			{
				var www = UnityWebRequest.Get(url);
				www.SendWebRequest();
				while (!www.isDone) await Task.Yield();
				if (www.result == UnityWebRequest.Result.Success)
				{
					FLog.Verbose("Downloaded privacy policy");
					data.Text = RemoveUnicodeAndColorCodes(www.downloadHandler.text);
				}
				else
				{
					FLog.Verbose("Using default privacy policy");
					data.Text = defaultValue;
				}
			}
		
			await _services.GameUiService.OpenUiAsync<GenericScrollingTextDialogPresenter, GenericScrollingTextDialogPresenter.StateData>(data);
		}

			
		private string RemoveUnicodeAndColorCodes(string text) {
			string cleanedText = Regex.Replace(text, @"\p{Cs}", ""); // Remove Unicode characters
			cleanedText = Regex.Replace(cleanedText, @"\u001B\[\d{1,2}m", ""); // Remove color codes (e.g., \u001B[32m)
			return cleanedText;
		}
		
	}

	

	public static class HardCodedTexts
	{
		public readonly static string PRIVACY_POLICY = @"<b> Privacy Policy </b>  WE VALUE YOUR PRIVACY
We value the privacy of individuals who visit our website at https://blastroyale.com/ (the “Website”), and any of our other applications, or services that link to this Privacy Policy (collectively, the 'Services'). This Privacy Policy ('Policy') is designed to explain how we collect, use, and share information from users of the Services. This Policy is incorporated by reference into our Terms of Use. By agreeing to this Policy through your continued use of the Services, you agree to the terms and conditions of this Policy.

WHAT TYPE OF INFORMATION WE COLLECT?

We collect any information you provide to us when you use the Services. You may provide us with information in various ways on the Services. Personal Information: When you use our Services, you are required to provide us with your name and email address.
Financial Information: We may collect necessary financial information such as your wallet address when you use our Services. Usage information: We may collect information about how you access and use our Services, your actions on the Services, including your interactions with others on the Services, comments and posts you make in our public discussion forums, and other content you may provide. Technical Data: We may collect data such as IP (internet protocol) address, ISP (internet Services provider), the web browser used to access the Services, the time the Services was accessed, which web pages were visited, operating system and platform, a mobile device-type identifier, and other technology on the devices you use to access our Services. We may also access your photo and camera roll or Face ID through our mobile application with your permission. Communications: We may receive additional information about you when you contact us directly. For example, we will receive your email address, the contents of a message or attachments that you may send to us, and other information you choose to provide when you contact us through email.

WE COLLECT COOKIES

When you use our Services, we may collect information from you through automated means, such as cookies, web beacons, and web server logs. By using the Services, you consent to the placement of cookies, beacons, and similar technologies in your browser and on emails in accordance with this Policy. The information collected in this manner includes IP address, browser characteristics, device IDs and characteristics, operating system version, language preferences, referring URLs, and information about the usage of our Services. We may use this information, for example, to ensure that the Services functions properly, to determine how many users have visited certain pages or opened messages, or to prevent fraud. We also work with analytics providers which use cookies and similar technologies to collect and analyze information about use of the Services and report on activities and trends. If you do not want information collected through the use of cookies, there is a procedure in most browsers that allows you to automatically decline cookies or be given the choice of declining or accepting the transfer to your computer of a particular cookie (or cookies) from a particular site. You may also wish to refer to http://www.allaboutcookies.org/manage-cookies/index.html. If, however, you do not accept cookies, you may experience some inconvenience in your use of the Services.
HOW DO WE USE THE INFORMATION WE COLLECT?
* Operating, maintaining, enhancing and providing features of the Services, providing Services and information that you request, responding to comments and questions, and otherwise providing support to users. 
* Understanding and analyzing the usage trends and preferences of our users, improving the Services, developing new products, services, features, and functionality. 
* Contacting you for administrative or informational purposes, including by providing customer services or sending communications, including changes to our terms, conditions, and policies. 
* For marketing purposes, such as developing and providing promotional and advertising materials that may be useful, relevant, valuable or otherwise of interest. 
* Personalizing your experience on the Services by presenting tailored content. 
* We may aggregate data collected through the Services and may use and disclose it for any purpose. 
* For our business purposes, such as audits, security, compliance with applicable laws and regulations, fraud monitoring and prevention. 
* Complying with legal and regulatory requirements.
* Protecting our interests, enforcing our Terms of Use or other legal rights.   
<b> WHEN AND WITH WHOM DO WE SHARE THE INFORMATION WE COLLECT? </b>

We do not rent, sell or share your information with third parties except as described in this Policy. We may share your information with the following:
* Entities in our group or our affiliates in order to provide you with the Services. 
* Our third-party services providers who provide services such as website hosting, data analysis, customer services, email delivery, auditing, and other services. 
* Credit bureaus and other third parties who provide Know Your Customer and Anti-Money Laundering services. 
* Potential or actual acquirer, successor, or assignee as part of any reorganization, merger, sale, joint venture, assignment, transfer or other disposition of all or any portion of our business, assets or stock (including in bankruptcy or similar proceedings). 
* If required to do so by law or in the good faith belief that such action is appropriate: (a) under applicable law, including laws outside your country of residence; (b) to comply with legal process; (c) to respond to requests from public and government authorities, including public and government authorities outside your country of residence; (d) to enforce our terms and conditions; (e) to protect our operations or those of any of our subsidiaries; (f) to protect our rights, privacy, safety or property, and/or that of our subsidiaries, you or others; and (g) to allow us to pursue available remedies or limit the damages that we may sustain. In addition, we may use third party analytics vendors to evaluate and provide us with information about your use of the Services. We do not share your information with these third parties, but these analytics services providers may set and access their own cookies, pixel tags and similar technologies on the services and they may otherwise collect or have access to information about you which they may collect over time and across different websites. For example, we use Google Analytics to collect and process certain analytics data. Google provides some additional privacy options described at https://www.google.com/policies/privacy/partners. We may use and disclose aggregate information that does not identify or otherwise relate to an individual for any purpose, unless we are prohibited from doing so under applicable law.   
<b> THIRD-PARTY SERVICES </b>

We may display third-party content on the Services. Third-party content may use cookies, web beacons, or other mechanisms for obtaining data in connection with your viewing of and/or interacting with the third-party content on the Services. You should be aware that there is always some risk involved in transmitting information over the internet. While we strive to protect your Personal Information, we cannot ensure or warrant the security and privacy of your Personal Information or other content you transmit using the Services, and you do so at your own risk. Please note that we cannot control, nor will we be responsible for the Personal Information collected and processed by third parties, its safekeeping or a breach thereof, or any other act or omission pertaining to it and their compliance with applicable privacy laws or regulations. We advise you to read the respective privacy policy of any such third party and use your best discretion.

HOW WE PROTECT YOUR PERSONAL INFORMATION

You acknowledge that no data transmission over the internet is totally secure. Accordingly, we cannot warrant the security of any information which you transmit to us. That said, we do use certain physical, organizational, and technical safeguards that are designed to maintain the integrity and security of information that we collect. You need to help us prevent unauthorized access to your account by protecting and limiting access to your account appropriately (for example, by logging out after you have finished accessing your account). You will be solely responsible for keeping your account against any unauthorized use. While we seek to protect your information to ensure that it is kept confidential, we cannot absolutely guarantee its security. However, we do not store any passwords as an added layer of security. Please be aware that no security measures are perfect or impenetrable and thus we cannot and do not guarantee the security of your data. While we strive to protect your Personal Information, we cannot ensure or warrant the security and privacy of your Personal Information or other content you transmit using the Services, and you do so at your own risk. It is important that you maintain the security and control of your account credentials.

HOW LONG DO WE KEEP YOUR INFORMATION?

We will retain your Information for as long as necessary to provide our Services, and as necessary to comply with our legal obligations (including those specific to financial transactions), resolve disputes, and enforce our policies. Retention periods will be determined taking into account the type of information that is collected and the purpose for which it is collected, bearing in mind the requirements applicable to the situation and the need to destroy outdated, unused information at the earliest reasonable time.

<b> YOUR RIGHTS </b>

You may, of course, decline to share certain information with us, in which case we may not be able to provide to you some of the features and functionality of the Services. From time to time, we send marketing e-mail messages to our users, including promotional material concerning our Services. If you no longer want to receive such emails from us on a going forward basis, you may opt-out via the 'unsubscribe' link provided in each such email.
NO USE OF SERVICES BY MINORS
The Services is not directed to individuals under the age of eighteen (18), and we request that you do not provide personal information through the Services if you are under 18.
CROSS-BORDER DATA TRANSFER
Please note that we may be transferring your information outside of your region for storage and processing and around the globe. By using the Services you consent to the transfer of information to countries outside of your country of residence, which may have data protection rules that are different from those of your country.
UPDATES TO THIS PRIVACY POLICY
We may make changes to this Policy. The 'Last Updated' date at the bottom of this page indicates when this Policy was last revised. If we make material changes, we may notify you through the Services or by sending you an email or other communication. The most current version will always be posted on our website. We encourage you to read this Policy periodically to stay up-to-date about our privacy practices. By continuing to access or use our Services after any revisions become effective, you agree to be bound by the updated Policy.
CONTACT US
If you have any questions about this Policy, please contact us at admin@firstlight.games
";

		public readonly static string TERMS_OF_SERVICE = 
@"<b>Terms of Service</b>  General These terms and conditions (“Terms”) govern the use of the Website (defined below) and the Services (defined below). These Terms also include any guidelines, announcements, additional terms, policies, and disclaimers made available or issued by us from time to time. These Terms constitute a binding and enforceable legal contract between Fun Dimensions Limited and its affiliates (“Company”, “Blast Royale”, “we”, “us”) and you, an end user of the services (“you” or “User”) at https://blastroyale.com/ (“Services”). By accessing, using or clicking on our website (and all related subdomains) or its mobile applications (“Website”) or accessing, using or attempting to use the Services, you agree that you have read, understood, and to are bound by these Terms and that you comply with the requirements listed herein. If you do not agree to all of these Terms or comply with the requirements herein, please do not access or use the Website or the Services. In addition, when using some features of the Services, you may be subject to specific additional terms and conditions applicable to those features. We may modify, suspend or discontinue the Website or the Services at any time and without notifying you. We may also change, update, add or remove provisions of these Terms from time to time. Any and all modifications or changes to these Terms will become effective upon publication on our Website or release to Users. Therefore, your continued use of our Services is deemed your acceptance of the modified Terms and rules. If you do not agree to any changes to these Terms, please do not access or use the Website or the Services. We note that these Terms between you and us do not enumerate or cover all rights and obligations of each party, and do not guarantee full alignment with needs arising from future development. Therefore, our privacy policy, platform rules, guidelines and all other agreements entered into separately between you and us are deemed supplementary terms that are an integral part of these Terms and shall have the same legal effect. Your use of the Website or Services is deemed your acceptance of any supplementary terms too.

2.
Eligibility By accessing, using or clicking on our Website and using or attempting to use our Services, you represent and warrant that: (a) as an individual, legal person, or other organization, you have full legal capacity and authority to agree and bind yourself to these Terms; (b) you are at least 18 or are of legal age to form a binding contract under applicable laws; (c) your use of the Services is not prohibited by applicable law, and at all times compliant with applicable law, including but not limited to regulations on anti-money laundering, anti-corruption, and counter- terrorist financing (“CTF”); (d) you have not been previously suspended or removed from using our Services; (e) if you act as an employee or agent of a legal entity, and enter into these Terms on their behalf, you represent and warrant that you have all the necessary rights and authorizations to bind such legal entity; and (f) you are solely responsible for use of the Services and, if applicable, for all activities that occur on or through your user account.

3.
Identity Verification We and our affiliates may, but are not obligated to, collect and verify information about you in order to keep appropriate record of our users, protect us and the community from fraudulent users, and identify traces of money laundering, terrorist financing, fraud and other financial crimes, or for other lawful purposes. We may require you to provide or verify additional information before permitting you to access, use or click on our Website and/or use or attempt to use our use or access any Service. We may also suspend, restrict, or terminate your access to our Website or any or all of the Services in the following circumstances: (a) if we reasonably suspect you of using our Website and Services in connection with any prohibited use or business; (b) your use of our Website or Services is subject to any pending litigation, investigation, or government proceeding and/or we perceive a heightened risk of legal or regulatory non-compliance associated with your activity; or (c) you take any action that we deem as circumventing our controls, including, but not limited to, abusing promotions which we may offer from time to time. In addition to providing any required information, you agree to allow us to keep a record of that information during the period for which your account is active and within five (5) years after your account is closed. You also authorize us to share your submitted information and documentation to third parties to verify the authenticity of such information. We may also conduct necessary investigations directly or through a third party to verify your identity or protect you and/or us from financial crimes, such as fraud, and to take necessary action based on the results of such investigations. We will collect, use and share such information in accordance with our privacy policy. If you provide any information to us, you must ensure that such information is true, complete, and timely updated when changed. If there are any grounds for believing that any of the information you provided is incorrect, false, outdated or incomplete, we reserve the right to send you a notice to demand correction, directly delete the relevant information, and as the case may be, terminate all or part of the Services we provide for you. You shall be fully liable for any loss or expense caused to us during your use of the Services. You hereby acknowledge and agree that you have the obligation to keep all the information accurate, update and correct at all times. We reserve the right to confiscate any and all funds that are found to be in violation of relevant and applicable AML or CFT laws and regulations, and to cooperate with the competent authorities when and if necessary.

4.
Restrictions You shall not access, use or click on our Website and/or use or attempt to use the Services in any manner except as expressly permitted in these Terms. Without limiting the generality of the preceding sentence, you may NOT: (a) use our Website or use the Services in any dishonest or unlawful manner, for fraudulent or malicious activities, or in any manner inconsistent with these Terms; (b) violate applicable laws or regulations in any manner; (c) infringe any proprietary rights, including but not limited to copyrights, patents, trademarks, or trade secrets of Blast Royale; (d) use our Website or use the Services to transmit any data or send or upload any material that contains viruses, Trojan horses, worms, time-bombs, keystroke loggers, spyware, adware, or any other harmful programmes or computer code designed to adversely affect the operation of any computer software or hardware; (e) use any deep linking, web crawlers, bots, spiders or other automatic devices, programs, scripts, algorithms or methods, or any similar or equivalent manual processes to access, obtain, copy, monitor, replicate or bypass the Website or the Services; (f) make any back-up or archival copies of the Website or any part thereof, including disassembling or de-compilation of the Website; (g) violate public interests, public morals, or the legitimate interests of others, including any actions that would interfere with, disrupt, negatively affect, or prohibit other Users from using our Website and the Services; (h) use the Services for market manipulation (such as pump and dump schemes, wash trading, self-trading, front running, quote stuffing, and spoofing or layering, regardless of whether prohibited by law); (i) attempt to access any part or function of the Website without authorization, or connect to the Website or Services or any Company servers or any other systems or networks of any the Services provided through the services by hacking, password mining or any other unlawful or prohibited means; (j) probe, scan or test the vulnerabilities of the Website or Services or any network connected to the properties, or violate any security or authentication measures on the Website or Services or any network connected thereto; (k) reverse look-up, track or seek to track any information of any other Users or visitors of the Website or Services; (l) take any actions that imposes an unreasonable or disproportionately large load on the infrastructure of systems or networks of the Website or Services, or the infrastructure of any systems or networks connected to the Website or Services; (m) use any devices, software or routine programs to interfere with the normal operation of any transactions of the Website or Services, or any other person’s use of the Website or Services; or (n) forge headers, impersonate, or otherwise manipulate identification, to disguise your identity or the origin of any messages or transmissions you send to Blast Royale or the Website. By accessing the Services, you agree that we have the right to investigate any violation of these Terms, unilaterally determine whether you have violated these Terms, and take actions under relevant regulations without your consent or prior notice.

5.
Termination Blast Royale may terminate, suspend, or modify your access to Website and/or the Services, or any portion thereof, immediately and at any point, at its sole discretion. Blast Royale will not be liable to you or to any third party for any termination, suspension, or modification of your access to the Services. Upon termination of your access to the Services, these Terms shall terminate, except for those clauses that expressly or are intended to survive termination or expiry.

6.
No Warranties and Limitation of Liabilities OUR SERVICES ARE PROVIDED ON AN 'AS IS' AND 'AS AVAILABLE' BASIS WITHOUT ANY REPRESENTATION OR WARRANTY, WHETHER EXPRESS, IMPLIED OR STATUTORY. YOU HEREBY ACKNOWLEDGE AND AGREE THAT YOU HAVE NOT RELIED UPON ANY OTHER STATEMENT OR AGREEMENT, WHETHER WRITTEN OR ORAL, WITH RESPECT TO YOUR USE AND ACCESS OF THE SERVICES. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTIES OF TITLE, MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND/OR NON-INFRINGEMENT. BLAST ROYALE DOES NOT MAKE ANY REPRESENTATIONS OR WARRANTIES THAT ACCESS TO THE WEBSITE, ANY PART OF THE SERVICES, INCLUDING MOBILE SERVICES, OR ANY OF THE MATERIALS CONTAINED THEREIN, WILL BE CONTINUOUS, UNINTERRUPTED, TIMELY, OR ERROR-FREE AND WILL NOT BE LIABLE FOR ANY LOSSES RELATING THERETO. BLAST ROYALE DOES NOT REPRESENT OR WARRANT THAT THE WEBSITE, THE SERVICES OR ANY MATERIALS OF BLAST ROYALE ARE ACCURATE, COMPLETE, RELIABLE, CURRENT, ERROR-FREE, OR FREE OF VIRUSES OR OTHER HARMFUL COMPONENTS. TO THE MAXIMUM EXTENT PERMITTED BY APPLICABLE LAW, NONE OF BLAST ROYALE OR ITS AFFILIATES AND THEIR RESPECTIVE SHAREHOLDERS, MEMBERS, DIRECTORS, OFFICERS, EMPLOYEES, ATTORNEYS, AGENTS, REPRESENTATIVES, SUPPLIERS OR CONTRACTORS WILL BE LIABLE FOR ANY DIRECT, INDIRECT, SPECIAL, INCIDENTAL, INTANGIBLE OR CONSEQUENTIAL LOSSES OR DAMAGES ARISING OUT OF OR RELATING TO: (a) ANY PERFORMANCE OR NON-PERFORMANCE OF THE SERVICES, OR ANY OTHER PRODUCT, SERVICE OR OTHER ITEM PROVIDED BY OR ON BEHALF OF BLAST ROYALE OR ITS AFFILIATES; (b) ANY AUTHORIZED OR UNAUTHORIZED USE OF THE WEBSITE OR SERVICES, OR IN CONNECTION WITH THIS AGREEMENT; (c) ANY INACCURACY, DEFECT OR OMISSION OF ANY DATA OR INFORMATION ON THE WEBSITE; (d) ANY ERROR, DELAY OR INTERRUPTION IN THE TRANSMISSION OF SUCH DATA; (e) ANY DAMAGES INCURRED BY ANY ACTIONS, OMISSIONS OR VIOLATIONS OF THESE TERMS BY ANY THIRD PARTIES; OR (f) ANY DAMAGE CAUSED BY ILLEGAL ACTIONS OF OTHER THIRD PARTIES OR ACTIONS WITHOUT AUTHORIZED BY BLAST ROYALE. EVEN IF BLAST ROYALE KNEW OR SHOULD HAVE KNOWN OF THE POSSIBILITY OF SUCH DAMAGES AND NOTWITHSTANDING THE FAILURE OF ANY AGREED OR OTHER REMEDY OF ITS ESSENTIAL PURPOSE, EXCEPT TO THE EXTENT OF A FINAL JUDICIAL DETERMINATION THAT SUCH DAMAGES WERE A RESULT OF OUR GROSS NEGLIGENCE, ACTUAL FRAUD, WILLFUL MISCONDUCT OR INTENTIONAL VIOLATION OF LAW OR EXCEPT IN JURISDICTIONS THAT DO NOT ALLOW THE EXCLUSION OR LIMITATION OF INCIDENTAL OR CONSEQUENTIAL DAMAGES. THIS PROVISION WILL SURVIVE THE TERMINATION OF THESE TERMS. WE MAKE NO WARRANTY AS TO THE MERIT, LEGALITY OR JURIDICAL NATURE OF ANY TOKEN SOLD ON OUR PLATFORM (INCLUDING WHETHER OR NOT IT IS CONSIDERED A SECURITY OR FINANCIAL INSTRUMENT UNDER ANY APPLICABLE SECURITIES LAWS).
7.
Intellectual Property All present and future copyright, title, interests in and to the Services, registered and unregistered trademarks, design rights, unregistered designs, database rights and all other present and future intellectual property rights and rights in the nature of intellectual property rights that exist in or in relation to the use and access of the Website and the Services are owned by or otherwise licensed to Blast Royale. Subject to your compliance with these Terms, we grant you a non- exclusive, non-sub license, and any limited license to merely use or access the Website and the Services in the permitted hereunder. Except as expressly stated in these Terms, nothing in these Terms should be construed as conferring any right in or license to our or any other third party’s intellectual rights. If and to the extent that any such intellectual property rights are vested in you by operation of law or otherwise, you agree to do any and all such acts and execute any and all such documents as we may reasonably request in order to assign such intellectual property rights back to us. You agree and acknowledge that all content on the Website must not be copied or reproduced, modified, redistributed, used, created for derivative works, or otherwise dealt with for any other reason without being granted a written consent from us. Third parties participating on the Website may permit us to utilise trademarks, copyrighted material, and other intellectual property associated with their businesses. We will not warrant or represent that the content of the Website does not infringe the rights of any third party.
8.
Independent Parties Blast Royale is an independent contractor but not an agent of you in the performance of these Terms. These Terms shall not be interpreted as facts or evidence of an association, joint venture, partnership or franchise between the parties.
9.
No Professional Advice All information provided on the Website and throughout our Services is for informational purposes only and should not be construed as professional advice. We do not provide investment advice and any content contained on the Website should not be considered as a substitute for tailored investment advice. Investing in digital assets is highly risky and may lead to a total loss of investment. You must have sufficient understanding of cryptographic tokens, token storage mechanisms (such as token wallets), and blockchain technology to appreciate the risks involved in dealing in digital assets. You understand and agree that the value of digital assets can be volatile, and we are not in any way responsible or liable for any losses you may incur by using or transferring digital assets in connection with our Services. You should not take, or refrain from taking, any action based on any information contained on the Website. Before you make any financial, legal, or other decisions involving our Services, you should seek independent professional advice from an individual who is licensed and qualified in the area for which such advice would be appropriate.
10.
Indemnification You agree to indemnify and hold harmless Blast Royale and its affiliates and their respective shareholders, members, directors, officers, employees, attorneys, agents, representatives, suppliers or contractors from and against any potential or actual claims, actions, proceedings, investigations, demands, suits, costs, expenses and damages (including attorneys’ fees, fines or penalties imposed by any regulatory authority) arising out of or related to: (a) your use of, or conduct in connection with, the Website or Services; (b) your breach or our enforcement of these Terms; or (c) your violation of any applicable law, regulation, or rights of any third party during your use of the Website or Services. If you are obligated to indemnify Blast Royale and its affiliates and their respective shareholders, members, directors, officers, employees, attorneys, agents, representatives, suppliers or contractors pursuant to these Terms, Blast Royale will have the right, in its sole discretion, to control any action or proceeding and to determine whether Blast Royale wishes to settle, and if so, on what terms. Your obligations under this indemnification provision will continue even after these Terms have expired or been terminated.
11.
Taxes As between us, you will be solely responsible to pay any and all sales, use, value- added and other taxes, duties, and assessments (except taxes on our net income) now or hereafter claimed or imposed by any governmental authority (collectively, “Taxes”) associated with your use of the Services. Except for income taxes levied on the Company, you: (i) will pay or reimburse us for all national, federal, state, local, or other taxes and assessments of any jurisdiction, including value-added taxes and taxes as required by international tax treaties, customs or other import or export taxes, and amounts levied in lieu thereof based on charges set, services performed or payments made hereunder, as are now or hereafter may be imposed under the authority of any national, state, local or any other taxing jurisdiction; and (ii) shall not be entitled to deduct the amount of any such taxes, duties or assessments from payments made to us pursuant to these Terms.
12.
Confidentiality You acknowledge that the Services contain Blast Royale’s and its affiliates’ trade secrets and confidential information. You agree to hold and maintain the Services in confidence, and not to furnish any other person any confidential information of the Services or the Website. You agree to use a reasonable degree of care to protect the confidentiality of the Services. You will not remove or alter any of Blast Royale’s or its affiliates’ proprietary notices. Your obligations under this provision will continue even after these Terms have expired or been terminated.
13.
Anti-Money Laundering Blast Royale expressly prohibits and rejects the use of the Website or the Services for any form of illicit activity, including money laundering, terrorist financing or trade sanctions violations. By using the Website or the Services, you represent that you are not involved in any such activity.
14.
Force Majeure Blast Royale has no liability to you if it is prevented from or delayed in performing its obligations or from carrying on its Services and business, by acts, events, omissions or accidents beyond its reasonable control, including, without limitation, strikes, failure of a utility service or telecommunications network, act of God, war, riot, civil commotion, malicious damage, compliance with any law or governmental order, rule, regulation, or direction.
15.
Jurisdiction and Governing Law The parties shall attempt in good faith to mutually resolve any and all disputes, whether of law or fact, and of any nature whatsoever arising from or with respect to these Terms. These Terms and any dispute or claim arising out of or in connection with the Services or the Website shall be governed by, and construed in accordance with, the laws of the British Virgin Islands. Any dispute that is not resolved after good faith negotiations may be referred by either party for final, binding resolution by arbitration under the arbitration rules of the British Virgin Islands International Arbitration Centre (“BVIIAC”) under the BVIIAC Administered Arbitration Rules in force when the notice of arbitration is submitted. The law of this arbitration clause shall be the laws of British Virgin Islands. The seat of arbitration shall be the British Virgin Islands. The number of arbitrators shall be one (1). The arbitration proceedings shall be conducted in English. Any Dispute arising out of or related to these Terms is personal to you and us and will be resolved solely through individual arbitration and will not be brought as a class arbitration, class action or any other type of representative proceeding. There will be no class arbitration or arbitration in which an individual attempts to resolve a dispute as a representative of another individual or group of individuals. Further, a dispute cannot be brought as a class or other type of representative action, whether within or outside of arbitration, or on behalf of any other individual or group of individuals.
16.
Severability If any provision of these Terms is determined by any court or other competent authority to be unlawful or unenforceable, the other provisions of these Terms will continue in effect. If any unlawful or unenforceable provision would be lawful or enforceable if part of it were deleted, that part will be deemed to be deleted, and the rest of the provision will continue in effect (unless that would contradict the clear intention of the clause, in which case the entirety of the relevant provision will be deemed to be deleted).
17.
Notices All notices, requests, demands, and determinations for us under these Terms (other than routine operational communications) shall be sent to [insert email address].
18.
Assignment You may not assign or transfer any right to use the Services or any of your rights or obligations under these Terms without prior written consent from Blast Royale, including any right or obligation related to the enforcement of laws or the change of control. Blast Royale may assign or transfer any or all of its rights or obligations under these Terms, in whole or in part, without notice or obtaining your consent or approval.
19.
Third Party Rights No third party shall have any rights to enforce any terms contained herein.
20.
Third Party Website Disclaimer Any links to third party websites from our Services does not imply endorsement by us of any product, service, information or disclaimer presented therein, nor do we guarantee the accuracy of the information contained on them. If you suffer loss from using such third party product and service, we will not be liable for such loss. In addition, since we have no control over the terms of use or privacy policies of third-party websites, you should carefully read and understand those policies. BY MAKING USE OF OUR SERVICES, YOU ACKNOWLEDGE AND AGREE THAT: (A) YOU ARE AWARE OF THE RISKS ASSOCIATED WITH TRANSACTIONS OF ENCRYPTED OR DIGITAL TOKENS OR CRYPTOCURRENCIES WITH A CERTAIN VALUE THAT ARE BASED ON BLOCKCHAIN AND CRYPTOGRAPHY TECHNOLOGIES AND ARE ISSUED AND MANAGED IN A DECENTRALIZED FORM (“DIGITIAL CURRENCIES”); (B) YOU SHALL ASSUME ALL RISKS RELATED TO THE USE OF THE SERVICES AND TRANSACTIONS OF DIGITAL CURRENCIES; AND (C) BLAST ROYALE SHALL NOT BE LIABLE FOR ANY SUCH RISKS OR ADVERSE OUTCOMES. AS WITH ANY ASSET, THE VALUES OF DIGITAL CURRENCIES ARE VOLATILE AND MAY FLUCTUATE SIGNIFICANTLY AND THERE IS A SUBSTANTIAL RISK OF ECONOMIC LOSS WHEN PURCHASING, HOLDING OR INVESTING IN DIGITAL CURRENCIES.
";
	}
}