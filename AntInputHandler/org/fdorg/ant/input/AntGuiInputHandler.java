package org.fdorg.ant.input;

import java.util.Vector;
import org.apache.tools.ant.BuildException;
import org.apache.tools.ant.input.InputHandler;
import org.apache.tools.ant.input.InputRequest;
import org.apache.tools.ant.input.MultipleChoiceInputRequest;

/**
 * @author Patrick Zahra
 */
public class AntGuiInputHandler implements InputHandler {
	
	public void handleInput(InputRequest request) throws BuildException {
		AntInputDialog dlg = new AntInputDialog(
			request.getPrompt(),
			request.getDefaultValue(),
			request instanceof MultipleChoiceInputRequest
				? ((MultipleChoiceInputRequest)request).getChoices()
				: null);
		request.setInput(dlg.result);
		System.out.println(dlg.result);
	}
	
	//for testing purposes
	public static void main(String[] a) {
		AntGuiInputHandler agih = new AntGuiInputHandler();
		Vector<String> items = new Vector<String>();
		items.add("one");
		items.add("two");
		items.add("three");
		MultipleChoiceInputRequest req = new MultipleChoiceInputRequest("prompt\nhere", items);
		req.setDefaultValue("one");
		agih.handleInput(req);
	}
}