public static class EmailTemplates
{
    public static string Shortlisted(string candidateName, string jobTitle, string companyName, string detailsUrl = null)
    {
        var urlHtml = string.IsNullOrEmpty(detailsUrl)
            ? ""
            : $"<p><a href=\"{detailsUrl}\" style=\"background:#1b6ec2;color:#fff;padding:8px 12px;border-radius:6px;text-decoration:none;\">View application</a></p>";

        return $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color:#222;"">
              <h2 style=""color:#1b6ec2;"">You are shortlisted for {jobTitle}</h2>
              <p>Hi {candidateName},</p>
              <p>Congratulations — you have been shortlisted for <strong>{jobTitle}</strong> at <strong>{companyName}</strong>.</p>
              <p>Please check your application dashboard for next steps.</p>
              {urlHtml}
              <p>Best regards,<br/>HireHub Team</p>
            </body>
            </html>";
    }

    public static string InterviewScheduled(string candidateName, string jobTitle, string companyName, DateTime interviewAtLocal, string locationOrLink = null)
    {
        var formatted = interviewAtLocal.ToString("f"); 
        var placeHtml = string.IsNullOrEmpty(locationOrLink)
            ? ""
            : $"<p><strong>Details:</strong> {locationOrLink}</p>";

        return $@"
            <html>
            <body style=""font-family: Arial, sans-serif; color:#222;"">
              <h2 style=""color:#1b6ec2;"">Interview scheduled for {jobTitle}</h2>
              <p>Hi {candidateName},</p>
              <p>Your interview for <strong>{jobTitle}</strong> at <strong>{companyName}</strong> has been scheduled.</p>
              <p><strong>Date & time:</strong> {formatted}</p>
              {placeHtml}
              <p>Please reply if you need to reschedule.</p>
              <p>Best regards,<br/>{companyName} / HireHub</p>
            </body>
            </html>";
    }
}
