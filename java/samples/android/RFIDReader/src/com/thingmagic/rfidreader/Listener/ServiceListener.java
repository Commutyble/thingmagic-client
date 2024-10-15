package com.thingmagic.rfidreader.Listener;

import java.io.FileInputStream;
import java.text.DecimalFormat;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;
import java.util.Timer;
import java.util.TimerTask;
import java.util.concurrent.ConcurrentHashMap;

import android.app.ProgressDialog;
import android.content.Context;
import android.os.AsyncTask;
import android.os.Environment;
import android.text.Html;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.RadioButton;
import android.widget.RadioGroup;
import android.widget.Spinner;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;

import com.thingmagic.ReadExceptionListener;
import com.thingmagic.ReadListener;
import com.thingmagic.Reader;
import com.thingmagic.ReaderException;
import com.thingmagic.ReaderUtil;
import com.thingmagic.SerialReader;
import com.thingmagic.SimpleReadPlan;
import com.thingmagic.TMConstants;
import com.thingmagic.TagProtocol;
import com.thingmagic.TagReadData;
import com.thingmagic.rfidreader.R;
import com.thingmagic.rfidreader.ReaderActivity;
import com.thingmagic.rfidreader.TagRecord;
import com.thingmagic.rfidreader.services.SettingsService;
import com.thingmagic.util.LoggerUtil;
import com.thingmagic.util.Utilities;
import android.app.AlertDialog;
import android.content.DialogInterface;

public class ServiceListener implements View.OnClickListener, View.OnFocusChangeListener {
	private static String TAG = "ServiceListener";
	private static EditText ntReaderField;
	private static EditText customReaderField;
	private static EditText readPowerView = null;
	private static EditText writePowerView = null;
	private static Spinner serialList = null;
	private static LinearLayout servicelayout;
	private static RadioGroup readerRadioGroup = null;
	private static RadioButton serialReaderRadioButton = null;
	private static RadioButton networkReaderRadioButton = null;
	private static RadioButton customReaderRadioButton = null;
	private static RadioButton syncReadRadioButton = null;
	private static RadioButton asyncReadSearchRadioButton = null;
	private static Button readButton = null;
	private static Button connectButton = null;
	private static TextView searchResultCount = null;
	private static TextView readTimeView = null;
	private static TextView totalTagCountView = null;

	private static TextView unique_tag_count = null;
	private static TextView total_tag_count = null;
	private static TextView time_taken = null;

	private static ProgressBar progressBar = null;
	private static int redColor = 0xffff0000;
	private static int textColor = 0xff000000;
	private static ReadThread readThread;
	private static FwUpdateThread fwUpdateThread;
	private static TableLayout table;
	private static LayoutInflater inflater;
	private static ArrayList<String> addedEPCRecords = new ArrayList<String>();;
	private static ConcurrentHashMap<String, TagRecord> epcToReadDataMap = new ConcurrentHashMap<String, TagRecord>();
	private static int uniqueRecordCount = 0;
	private static int totalTagCount = 0;
	private static long queryStartTime = 0;
	private static long queryStopTime = 0;

	private static ReaderActivity mReaderActivity;
	private static SettingsService mSettingsService;
	private static Timer timer = new Timer();
	//Check boxes
	private static CheckBox antChkBox1 = null;
	private static CheckBox antChkBox2 = null;
	private static CheckBox antChkBox3 = null;
	private static CheckBox antChkBox4 = null;
	private static LinkedHashSet<Integer> antennaList = new LinkedHashSet<Integer>();
	private static EditText fwPathTxt = null;
	private static Button fwUpdateButton = null;
	private static Boolean fwUpdateStatus = false;
	private static Button homeButton = null;
	private static ProgressDialog pDialog =null;
	private static String pathFwPrefix = null;

	public ServiceListener(ReaderActivity readerActivity) {
		mReaderActivity = readerActivity;
		mSettingsService = new SettingsService(mReaderActivity);
		findAllViewsById();
		// Register onFocusChangeListener for readPowerView
		readPowerView.setOnFocusChangeListener(this);
		// Register onFocusChangeListener for writePowerView
		writePowerView.setOnFocusChangeListener(this);
		antChkBox1.setOnClickListener(new OnClickListener() {
			@Override
			public void onClick(View view) {
				onAntCheckboxClicked(view);
			}
		});
		antChkBox2.setOnClickListener(new OnClickListener() {
			@Override
			public void onClick(View view) {
				onAntCheckboxClicked(view);
			}
		});
		antChkBox3.setOnClickListener(new OnClickListener() {
			@Override
			public void onClick(View view) {
				onAntCheckboxClicked(view);
			}
		});
		antChkBox4.setOnClickListener(new OnClickListener() {
			@Override
			public void onClick(View view) {
				onAntCheckboxClicked(view);
			}
		});
		// Progress dialog
		pDialog = new ProgressDialog(mReaderActivity);
		pDialog.setCancelable(false);
		// Set downloads path to edit text to tell user to download and pick fw file
		pathFwPrefix = Environment.getExternalStorageDirectory().toString() + "/Download/";
		fwUpdateButton.setOnClickListener(new OnClickListener() {
			@Override
			public void onClick(View v) {
				// update fw
				String pathRetrieved = fwPathTxt.getText().toString();
				fwUpdateThread = new FwUpdateThread(mReaderActivity.reader, pathRetrieved);
				fwUpdateThread.execute();
			}
		});
	}

	private void findAllViewsById() {
		syncReadRadioButton = (RadioButton) mReaderActivity
				.findViewById(R.id.SyncRead_radio_button);
		asyncReadSearchRadioButton = (RadioButton) mReaderActivity
				.findViewById(R.id.AsyncRead_radio_button);
		readButton = (Button) mReaderActivity.findViewById(R.id.Read_button);
		connectButton = (Button) mReaderActivity
				.findViewById(R.id.Connect_button);
		searchResultCount = (TextView) mReaderActivity
				.findViewById(R.id.search_result_view);
		totalTagCountView = (TextView) mReaderActivity
				.findViewById(R.id.totalTagCount_view);
		progressBar = (ProgressBar) mReaderActivity
				.findViewById(R.id.progressbar);
		textColor = searchResultCount.getTextColors().getDefaultColor();
		table = (TableLayout) mReaderActivity.findViewById(R.id.tablelayout);
		inflater = (LayoutInflater) mReaderActivity
				.getSystemService(Context.LAYOUT_INFLATER_SERVICE);
		ntReaderField = (EditText) mReaderActivity
				.findViewById(R.id.search_edit_text);
		customReaderField = (EditText) mReaderActivity
				.findViewById(R.id.custom_reader_field);
		serialList = (Spinner) mReaderActivity.findViewById(R.id.SerialList);
		serialReaderRadioButton = (RadioButton) mReaderActivity
				.findViewById(R.id.SerialReader_radio_button);
		networkReaderRadioButton = (RadioButton) mReaderActivity
				.findViewById(R.id.NetworkReader_radio_button);
		customReaderRadioButton = (RadioButton) mReaderActivity
				.findViewById(R.id.CustomReader_radio_button);
		servicelayout = (LinearLayout) mReaderActivity
				.findViewById(R.id.ServiceLayout);
		readerRadioGroup = (RadioGroup) mReaderActivity
				.findViewById(R.id.Reader_radio_group);
		readTimeView = (TextView) mReaderActivity.readOptions
				.findViewById(R.id.read_time_value);
		readPowerView = (EditText) mReaderActivity.readOptions
				.findViewById(R.id.read_power_value);
		writePowerView = (EditText) mReaderActivity.readOptions
				.findViewById(R.id.write_power_value);
		unique_tag_count = (TextView) mReaderActivity.performance_metrics
				.findViewById(R.id.unique_tag_count);
		total_tag_count = (TextView) mReaderActivity.performance_metrics
				.findViewById(R.id.total_tag_count);
		time_taken = (TextView) mReaderActivity.performance_metrics
				.findViewById(R.id.time_taken);
		antChkBox1=(CheckBox)mReaderActivity.readOptions.findViewById(R.id.antCheckBox1);
		antChkBox2=(CheckBox)mReaderActivity.readOptions.findViewById(R.id.antCheckBox2);
		antChkBox3=(CheckBox)mReaderActivity.readOptions.findViewById(R.id.antCheckBox3);
		antChkBox4=(CheckBox)mReaderActivity.readOptions.findViewById(R.id.antCheckBox4);
		fwPathTxt = (EditText) mReaderActivity.firmware.findViewById(R.id.editText_path);
		fwUpdateButton = (Button)mReaderActivity.firmware.findViewById(R.id.button_fw_update);
		homeButton =(Button) mReaderActivity.findViewById(R.id.btn_back_main);
	}

	public void onAntCheckboxClicked(View view) {
		// Check which checkbox was clicked
		switch(view.getId()) {
			case R.id.antCheckBox1:
				if (antChkBox1.isChecked())
					antennaList.add(1);
				else
					antennaList.remove(1);
			case R.id.antCheckBox2:
				if (antChkBox2.isChecked())
					antennaList.add(2);
				else
					antennaList.remove(2);
			case R.id.antCheckBox3:
				if (antChkBox3.isChecked())
					antennaList.add(3);
				else
					antennaList.remove(3);
			case R.id.antCheckBox4:
				if (antChkBox4.isChecked())
					antennaList.add(4);
				else
					antennaList.remove(4);
		}
	}

	@Override
	public void onClick(View arg0) {
		try {
			// Convert Set to Integer[]
			Integer[] antArray = antennaList.toArray(new Integer[antennaList.size()]);
			int[] antList = new int[antArray.length];
			// Convert Integer[] to int[]
			for(Integer i= 0 ; i < antArray.length; i++)
			{
				antList[i] = antArray[i].intValue();
			}

			SimpleReadPlan  simplePlan = new SimpleReadPlan(antList, TagProtocol.GEN2);
			mReaderActivity.reader.paramSet("/reader/read/plan", simplePlan);

			String operation = "";
			if (syncReadRadioButton.isChecked()) {
				operation = "syncRead";
				String readTimout = readTimeView.getText().toString();
				if (!Utilities.validateReadTimeout(searchResultCount,
						readTimout)) {
					return;
				}
				readButton.setText("Reading");
				readButton.setClickable(false);
			} else if (asyncReadSearchRadioButton.isChecked()) {
				operation = "asyncRead";
			}

			if (readButton.getText().equals("Stop Reading")) {
				readThread.setReading(false);
				readButton.setText("Stopping...");
				readButton.setClickable(false);
			} else if (readButton.getText().equals("Start Reading") || readButton.getText().equals("Reading")) {
				if (readButton.getText().equals("Start Reading")) {
					readButton.setText("Stop Reading");
				}
				clearTagRecords();
				readThread = new ReadThread(mReaderActivity.reader, operation);
				readThread.execute();
			}
		} catch (Exception ex) {
			LoggerUtil.error(TAG, "Exception", ex);
		}
	}

	public OnClickListener clearListener = new OnClickListener() {

		@Override
		public void onClick(View v) {
			clearTagRecords();
		}
	};

	public static void clearTagRecords() {
		addedEPCRecords.clear();
		epcToReadDataMap.clear();
		table.removeAllViews();
		searchResultCount.setText("");
		totalTagCountView.setText("");
		uniqueRecordCount = 0;
		totalTagCount = 0;
		queryStartTime = System.currentTimeMillis();
	}

	@Override
	public void onFocusChange(View view, boolean hasFocus) {
		if(!hasFocus)
		{
			try
			{
				// Read Power and write Power set
				String rdPow = readPowerView.getText().toString();
				String wrtPow = writePowerView.getText().toString();
				if(view.getId() == R.id.read_power_value && rdPow != null) {
					int readPowerToSet = (int) (Float.parseFloat(rdPow) * 100);
					mReaderActivity.reader.paramSet(TMConstants.TMR_PARAM_RADIO_READPOWER, readPowerToSet);
				}
				if(view.getId() == R.id.write_power_value && wrtPow != null) {
					int writePowerToSet = (int) (Float.parseFloat(wrtPow) * 100);
					mReaderActivity.reader.paramSet(TMConstants.TMR_PARAM_RADIO_WRITEPOWER, writePowerToSet);
				}
			}catch(ReaderException ex)
			{
				try {
					// Exception occurred while setting either read power or write power. Retrieve max and min power limits and alert user.
					int minReaderPower = (Integer) mReaderActivity.reader.paramGet("/reader/radio/powerMin");
					int maxReaderPower = (Integer) mReaderActivity.reader.paramGet("/reader/radio/powerMax");

					String message = "Please enter power within " + (String.valueOf(minReaderPower / 100.0)) + " and " + (String.valueOf(maxReaderPower / 100.0)) + " dBm";
					AlertDialog powerAlert = new AlertDialog.Builder(mReaderActivity)
							.setIcon(android.R.drawable.ic_dialog_alert)
							.setTitle("RFIDReader Message")
							.setMessage(message)
							.setPositiveButton("Ok", new DialogInterface.OnClickListener() {
								@Override
								public void onClick(DialogInterface dialog, int which) {
									try {
										// Reset power levels back to original ones upon error in UI. Fetch module power and set to UI
										Integer readPower = (Integer) mReaderActivity.reader.paramGet(TMConstants.TMR_PARAM_RADIO_READPOWER);
										Float readPowerValue = (float) readPower / 100;
										readPowerView.setText(readPowerValue.toString());

										// Retrieve the module write power and display
										Integer writePower = (Integer) mReaderActivity.reader.paramGet(TMConstants.TMR_PARAM_RADIO_WRITEPOWER);
										Float writePowerValue = (float) writePower / 100;
										writePowerView.setText(writePowerValue.toString());
									}catch(ReaderException re)
									{
										LoggerUtil.error(TAG, "Error :", re);
									}
								}
							})
							.setCancelable(false)
							.show();
					LoggerUtil.error(TAG, "Error :", ex);
				}
				catch(ReaderException re)
				{
					LoggerUtil.error(TAG, "Error :", re);
				}
			}
		}
	}

	public static class FwUpdateThread extends  AsyncTask<Void, Integer, Boolean>
	{
		private static Reader mReader;
		private String fwPath;
		private FileInputStream fileStream;
		String exceptionMsg = null;
		public FwUpdateThread(Reader reader, String fwPath) {
			this.fwPath = fwPath;
			mReader = reader;
		}

		@Override
		protected void onPreExecute() {
			LoggerUtil.debug(TAG, "** onPreExecute of Fw Update Thread**");
			pDialog.setProgressStyle(ProgressDialog.STYLE_SPINNER);
			//Disable buttons
			fwPathTxt.setEnabled(false);
			fwUpdateButton.setEnabled(false);
			homeButton.setEnabled(false);
			readPowerView.setEnabled(false);
			writePowerView.setEnabled(false);
			readTimeView.setEnabled(false);
			pDialog.setMessage("Firmware Update is in progress. Please wait...");
			pDialog.show();
			antChkBox1.setEnabled(false);
			antChkBox2.setEnabled(false);
			antChkBox3.setEnabled(false);
			antChkBox4.setEnabled(false);
		}

		@Override
		protected Boolean doInBackground(
				Void... params) {
			try {
				fwUpdateStatus = false;
				fileStream = new FileInputStream(fwPath);
				mReaderActivity.reader.firmwareLoad(fileStream);
				fileStream.close();
				publishProgress(0);
				fwUpdateStatus = true;
			}
			catch(Exception ex)
			{
				fwUpdateStatus = false;
				exceptionMsg = ex.getMessage();
				LoggerUtil.error(TAG, "Error :", ex);
			}
			finally
			{
				return fwUpdateStatus;
			}
		}

		@Override
		protected void onProgressUpdate(Integer... progress) {
			int progressToken = progress[0];
			if (progressToken == -1) {
				// Got exception , show to user
				LoggerUtil.error(TAG, "FW update failed");
			} else {
				//succeeded operation
				LoggerUtil.error(TAG, "FW update succeeded");
			}
		}

		@Override
		protected void onPostExecute(Boolean fwUpdateStatus){
			pDialog.dismiss();
			String message = "";

			if(fwUpdateStatus) {
				message = "Firmware Update is successful.";
			}
			else {
				message = "Firmware Update Failed. " + exceptionMsg;
			}
			AlertDialog powerAlert = new AlertDialog.Builder(mReaderActivity)
					.setIcon(android.R.drawable.ic_dialog_alert)
					.setTitle("RFIDReader Message")
					.setMessage(message)
					.setPositiveButton("Ok", new DialogInterface.OnClickListener() {
						@Override
						public void onClick(DialogInterface dialog, int which) {
							try {
								System.out.println("Onclick of alert dialog fw update");
							}catch(Exception re)
							{
								LoggerUtil.error(TAG, "Error :", re);
							}
						}
					})
					.setCancelable(false)
					.show();
			//Enable buttons
			fwPathTxt.setEnabled(true);
			fwUpdateButton.setEnabled(true);
			homeButton.setEnabled(true);
			readPowerView.setEnabled(true);
			writePowerView.setEnabled(true);
			readTimeView.setEnabled(true);
			antChkBox1.setEnabled(true);
			antChkBox2.setEnabled(true);
			antChkBox3.setEnabled(true);
			antChkBox4.setEnabled(true);
			fwPathTxt.setText(pathFwPrefix);
		}
	}

	public static class ReadThread extends
			AsyncTask<Void, Integer, ConcurrentHashMap<String, TagRecord>> {

		private String operation;
		private static boolean exceptionOccur = false;
		private static String exception = "";
		private static boolean reading = true;
		private static Reader mReader;
		private static TableRow fullRow = null;
		private static TextView nr = null;
		private static TextView epcValue = null;
		private static TextView dataView = null;
		private static TextView countView = null;
		private static boolean isEmbeddedRead = false;
		private static double timeTaken;
		private static long startTime;
		static ReadExceptionListener exceptionListener = new TagReadExceptionReceiver();
		static ReadListener readListener = new PrintListener();

		public ReadThread(Reader reader, String operation) {
			this.operation = operation;
			mReader = reader;
		}

		@Override
		protected void onPreExecute() {
			startTime = System.currentTimeMillis();
			clearTagRecords();
			syncReadRadioButton.setClickable(false);
			asyncReadSearchRadioButton.setClickable(false);
			connectButton.setEnabled(false);
			connectButton.setClickable(false);
			searchResultCount.setTextColor(textColor);
			searchResultCount.setText("Reading Tags....");
			progressBar.setVisibility(View.VISIBLE);
			addedEPCRecords = new ArrayList<String>();
			epcToReadDataMap = new ConcurrentHashMap<String, TagRecord>();
			exceptionOccur = false;

		}

		@Override
		protected ConcurrentHashMap<String, TagRecord> doInBackground(
				Void... params) {
			try {
				//mSettingsService.loadReadPlan(mReader);
				if (operation.equalsIgnoreCase("syncRead")) {
					// Enable read filter in sync read
					mReader.paramSet("/reader/tagReadData/enableReadFilter", true);
					int timeOut = Integer.parseInt(readTimeView.getText()
							.toString());
					TagReadData[] tagReads = mReader.read(timeOut);
					queryStopTime = System.currentTimeMillis();
					for (TagReadData tr : tagReads) {
						parseTag(tr, false);
					}
					publishProgress(0);
				} else {
					// Disable read filter in async read
					mReader.paramSet("/reader/tagReadData/enableReadFilter", false);
					setReading(true);
					mReader.addReadExceptionListener(exceptionListener);
					mReader.addReadListener(readListener);
					mReader.startReading();
					queryStartTime = System.currentTimeMillis();
					refreshReadRate();
					while (isReading()) {
						/* Waiting till stop reading button is pressed */
						Thread.sleep(5);
					}
					queryStopTime = System.currentTimeMillis();
					if(!exceptionOccur)
					{
						mReader.stopReading();
						mReader.removeReadListener(readListener);
						mReader.removeReadExceptionListener(exceptionListener);
					}
				}
			} catch (Exception ex) {
				exception = ex.getMessage();
				exceptionOccur = true;
				LoggerUtil.error(TAG, "Exception while reading :", ex);
			}

			return epcToReadDataMap;
		}

		static class PrintListener implements ReadListener {
			public void tagRead(Reader r, final TagReadData tr) {
				readThread.parseTag(tr, true);
			}
		}

		// private static int connectionLostCount=0;
		static class TagReadExceptionReceiver implements ReadExceptionListener {
			public void tagReadException(Reader r, ReaderException re) {
				if (re.getMessage().contains("The module has detected high return loss")
						|| re.getMessage().contains("Tag ID buffer full") 
						|| re.getMessage().contains("No tags found")) {
					// exception = "No connected antennas found";
					/* Continue reading */
				}
				// else if(re.getMessage().equals("Connection Lost"))
				// {
				// if(connectionLostCount == 3){
				// connectionLostCount = 0;
				// try {
				// r.connect();
				// } catch (Exception e) {
				// // TODO Auto-generated catch block
				// e.printStackTrace();
				// exception=re.getMessage();
				// exceptionOccur = true;
				// readThread.setReading(false);
				// readThread.publishProgress(-1);
				// }
				// }
				// connectionLostCount++;
				// }
				else if(re.getMessage().contains("Timeout"))
				{
					Log.e(TAG, "Reader exception : ", re);
					exception = re.getMessage();
					exceptionOccur = true;
					readThread.setReading(false);
					readThread.publishProgress(-1);
					r.destroy();
					r = null;
					disconnectReader();
				}
				else {
					Log.e(TAG, "Reader exception : ", re);
					exception = re.getMessage();
					exceptionOccur = true;
					readThread.setReading(false);
					readThread.publishProgress(-1);
				}
			}
		}

	private void refreshReadRate() {
		    timer = new Timer();
			timer.schedule( new TimerTask() {
				  @Override
				  public void run() {
					  publishProgress(0);
				  }
				}, 100, 300);	
		}

		// private static void calculateReadrate()
		// {
		// long readRatePerSec = 0;
		// long elapsedTime = (System.currentTimeMillis() - queryStartTime) ;
		// if(!isReading()){
		// elapsedTime = queryStopTime- queryStartTime;
		// }
		//
		// long tagReadTime = elapsedTime/ 1000;
		// if(tagReadTime == 0)
		// {
		// readRatePerSec = (long) ((totalTagCount) / ((double) elapsedTime /
		// 1000));
		// }
		// else
		// {
		// readRatePerSec = (long) ((totalTagCount) / (tagReadTime));
		// }
		// }

		private void parseTag(TagReadData tr, boolean publishResult) {
			totalTagCount += tr.getReadCount();
			String epcString = tr.getTag().epcString();
			if (epcToReadDataMap.keySet().contains(epcString)) {
				TagRecord tempTR = epcToReadDataMap.get(epcString);
				tempTR.readCount += tr.getReadCount();
			} else {
				TagRecord tagRecord = new TagRecord();
				tagRecord.setEpcString(epcString);
				tagRecord.setReadCount(tr.getReadCount());
				epcToReadDataMap.put(epcString, tagRecord);
			}
		}

		@Override
		protected void onProgressUpdate(Integer... progress) {
			int progressToken = progress[0];
			if (progressToken == -1) {
				searchResultCount.setTextColor(redColor);
				searchResultCount.setText("ERROR :" + exception);
				totalTagCountView.setText("");
			} else {
				populateSearchResult(epcToReadDataMap);
				if (!exceptionOccur && totalTagCount > 0) {
					searchResultCount.setTextColor(textColor);
					searchResultCount.setText(Html
							.fromHtml("<b>Unique Tags :</b> "
									+ epcToReadDataMap.keySet().size()));
					totalTagCountView.setText(Html
							.fromHtml("<b>Total Tags  :</b> " + totalTagCount));
				}
			}
		}

		@Override
		protected void onPostExecute(
				ConcurrentHashMap<String, TagRecord> epcToReadDataMap) {
			timer.cancel();
			if (exceptionOccur) {
				searchResultCount.setTextColor(redColor);
				searchResultCount.setText("ERROR :" + exception);
				totalTagCountView.setText("");
				if (totalTagCount > 0 && !operation.equalsIgnoreCase("syncRead")) {	
					if(exception.length() > 20)
					{
						totalTagCountView.setText(Html
								.fromHtml("<br>"));
					}
					totalTagCountView.setText(Html
							.fromHtml("<b>Total Tags  :</b> " + totalTagCount));
					}
			} else {
				searchResultCount.setText(Html.fromHtml("<b>Unique Tags :</b> "
						+ epcToReadDataMap.keySet().size()));
				totalTagCountView.setText(Html.fromHtml("<b>Total Tags  :</b> "
						+ totalTagCount));
				populateSearchResult(epcToReadDataMap);
				System.out.println("unique_tag_count :" + unique_tag_count);
				unique_tag_count.setText(Integer.toString(epcToReadDataMap
						.keySet().size()));
				total_tag_count.setText(Integer.toString(totalTagCount));
				long elapsedTime = queryStopTime - queryStartTime;
				double timeTaken = (double) ((totalTagCount) / ((double) elapsedTime / 1000));
				DecimalFormat df = new DecimalFormat("#.##");
				time_taken.setText(df.format(timeTaken) + " sec");
			}
			progressBar.setVisibility(View.INVISIBLE);
			readButton.setClickable(true);
			if (operation.equalsIgnoreCase("AsyncRead")) {
				readButton.setText("Start Reading");
			} else if (operation.equalsIgnoreCase("SyncRead")) {
				readButton.setText("Read");
			}
			readButton.setClickable(true);
			syncReadRadioButton.setClickable(true);
			asyncReadSearchRadioButton.setClickable(true);
			connectButton.setClickable(true);
			connectButton.setEnabled(true);
			if (exceptionOccur && !exception.equalsIgnoreCase("No connected antennas found")) {
				disconnectReader();
			}
		}

		private static void disconnectReader() {
			ntReaderField.setEnabled(true);
			serialList.setEnabled(true);
			customReaderField.setEnabled(true);
			serialReaderRadioButton.setClickable(true);
			networkReaderRadioButton.setClickable(true);
			customReaderRadioButton.setClickable(true);
			connectButton.setText("Connect");
			servicelayout.setVisibility(View.GONE);
			readerRadioGroup.setVisibility(View.VISIBLE);
			mReaderActivity.reader = null;
			if(!exceptionOccur)
			{
				searchResultCount.setText("");
				total_tag_count.setText("");
			}
		}

		private static void populateSearchResult(
				ConcurrentHashMap<String, TagRecord> epcToReadDataMap) {
			try {
				Set<String> keySet = epcToReadDataMap.keySet();
				for (String epcString : keySet) {
					TagRecord tagRecordData = epcToReadDataMap.get(epcString);

					if (!addedEPCRecords.contains(epcString.toString())) {
						addedEPCRecords.add(epcString.toString());
						uniqueRecordCount = addedEPCRecords.size();
						if (inflater != null) {
							fullRow = (TableRow) inflater.inflate(R.layout.row,
									null, true);
							fullRow.setId(uniqueRecordCount);

							if (fullRow != null) {
								nr = (TextView) fullRow.findViewById(R.id.nr);
								if (nr != null) {
									nr.setText(String
											.valueOf(uniqueRecordCount));
									nr.setWidth(mReaderActivity.rowNumberWidth);
									epcValue = (TextView) fullRow
											.findViewById(R.id.EPC);

									if (epcValue != null) {
										epcValue.setText(tagRecordData
												.getEpcString());
										epcValue.setMaxWidth(mReaderActivity.epcDataWidth);
										countView = (TextView) fullRow
												.findViewById(R.id.COUNT);
										if (countView != null) {
											countView.setText(String
													.valueOf(tagRecordData
															.getReadCount()));
											countView
													.setWidth(mReaderActivity.epcCountWidth);
										}
										if (isEmbeddedRead) {
											dataView = (TextView) fullRow
													.findViewById(R.id.DATA);
											if (dataView != null) {
												dataView.setVisibility(View.VISIBLE);
												dataView.setText(String
														.valueOf(tagRecordData
																.getData()));
											}

										}
										table.addView(fullRow);
									}
								}
							}
						}
					} else {
						fullRow = (TableRow) table.getChildAt(Integer
								.valueOf(addedEPCRecords.indexOf(epcString)));
						if (fullRow != null) {
							countView = (TextView) fullRow.getChildAt(3);
							if (countView != null
									&& Integer.valueOf(countView.getText()
											.toString()) != tagRecordData
											.getReadCount()) {
								countView.setText(String.valueOf(tagRecordData
										.getReadCount()));
							}
						}
					}
				}
			} catch (Exception ex) {
				LoggerUtil.error(TAG, "Exception while populating tags :", ex);
			}
		}

		public static boolean isReading() {
			return reading;
		}

		public void setReading(boolean reading) {
			ReadThread.reading = reading;
		}
	}
}
