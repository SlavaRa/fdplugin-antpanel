package org.fdorg.ant.input;

import java.awt.BorderLayout;
import java.awt.Dimension;
import java.awt.Point;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.util.Vector;
import javax.swing.*;

/**
 * @author Patrick Zahra
 */
public class AntInputDialog extends JDialog implements ActionListener {
	private JButton btnOk, btnCancel;
	private JTextField txtInput;
	private JComboBox<String> lstInput;
	private JLabel lblInput;
	public String result;
	
	public AntInputDialog(String label, String def, Vector<String> choices) {
		super((JFrame)null, "Ant Input", true);
		
		JPanel panel = new JPanel();
		Boolean wait = false;
		if (label == "") {
			label = "Press Return key to continue...";
			wait = true;
		}
		lblInput = new JLabel(label);
		panel.add(lblInput);
		getContentPane().add(panel, BorderLayout.NORTH);

		panel = new JPanel();
		txtInput = new JTextField(def, 20);
		if (choices != null) {
			if (choices.size() == 0 || choices.get(0) == "") {
				wait = true;
			} else {
				lstInput = new JComboBox<String>(choices);
				if (def != "") lstInput.setSelectedItem(def);
				lstInput.addActionListener(this);
				panel.add(lstInput);
			}
		} else if (!wait) {
			panel.add(txtInput);
		}
		getContentPane().add(panel);
		
		panel = new JPanel();
		btnCancel = new JButton("Cancel"); 
		if (!wait) {
			panel.add(btnCancel); 
			btnCancel.addActionListener(this);
		}
		btnOk = new JButton("OK"); 
		panel.add(btnOk); 
		btnOk.addActionListener(this); 
		getContentPane().add(panel, BorderLayout.SOUTH);
		
		getRootPane().setDefaultButton(btnOk);
		setDefaultCloseOperation(DISPOSE_ON_CLOSE);
		pack(); 
		setVisible(true);
	}
	
	public void actionPerformed(ActionEvent e) {
		if (e.getSource() == lstInput) {
			txtInput.setText((String)lstInput.getSelectedItem());
		}
		if (e.getSource() == btnOk) {
			result = txtInput.getText();
			setVisible(false); 
			dispose(); 
		}
		if (e.getSource() == btnCancel) {
			setVisible(false); 
			dispose(); 
		}
	}
}